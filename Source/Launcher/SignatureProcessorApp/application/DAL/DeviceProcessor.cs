using Devices.Common;
using Devices.Verifone;
using Devices.Verifone.VIPA;
using SignatureProcessorApp.application.DAL.Helpers;
using SignatureProcessorApp.devices.common;
using SignatureProcessorApp.devices.common.Helpers;
using SignatureProcessorApp.devices.Verifone.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XO.Requests;

namespace SignatureProcessorApp.application.DAL
{
    public class DeviceProcessor : IDisposable
    {
        const int signatureCaptureTimeout = 10000;

        private VerifoneDevice device;
        private List<byte[]> HTMLValueBytes;
        private MemoryStream signatureCapture;

        private static ManualResetEvent resetEvent = new ManualResetEvent(false);

        public DeviceProcessor()
        {
            device = new VerifoneDevice();
        }

        public void Dispose()
        {
            if (device != null)
            {
                device.Dispose();
            }

            if (HTMLValueBytes is { })
            {
                foreach (var array in HTMLValueBytes)
                {
                    ArrayPool<byte>.Shared.Return(array, true);
                }
            }
        }

        public MemoryStream GetCardholderSignature()
        {
            signatureCapture = null;

            DeviceConfig deviceConfig = new DeviceConfig()
            {
                Valid = true,
                SupportedTransactions = new SupportedTransactions()
                {
                    EnableContactEMV = false,
                    EnableContactlessEMV = false,
                    EnableContactlessMSR = true,
                    ContactEMVConfigIsValid = true,
                    EMVKernelValidated = true
                }
            };

            DeviceInformation currentDeviceInformation = new DeviceInformation()
            {
                ComPort = "COM33"
            };

            bool active = false;
            device?.Probe(deviceConfig, currentDeviceInformation, out active);

            if (active)
            {
                LinkRequest linkRequest = RequestBuilder.LinkGetSignatureRequest();

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(signatureCaptureTimeout);
                cancellationTokenSource.CancelAfter(signatureCaptureTimeout);

                resetEvent.Reset();
                ThreadPool.QueueUserWorkItem(new WaitCallback(DoSignatureWork), new object[]{ linkRequest, cancellationTokenSource });
                resetEvent.WaitOne();

                cancellationTokenSource.Dispose();

                //HTMLResponseObject htmlResponse = new HTMLResponseObject(linkRequest.Actions[0].DALRequest.LinkObjects.SignatureData);

                //(HTMLResponseObject htmlResponseObject, int VipaResponse) htmlResponseObject = (htmlResponse, (int)VipaSW1SW2Codes.Success);

                //if (htmlResponseObject.VipaResponse == (int)VipaSW1SW2Codes.Success)
                //{
                //    // Target the second output in this case
                //    if (htmlResponseObject.htmlResponseObject?.SignatureData?.Count >= 1)
                //    {
                //        int offset = htmlResponseObject.htmlResponseObject.SignatureData.Count == 1 ? 0 : 1;
                //        byte[] prunedArray = SignaturePointsConverterHelper.PruneByteArray(htmlResponseObject.htmlResponseObject.SignatureData[offset]);

                //        //HTMLValueBytes = htmlResponseObject.htmlResponseObject.SignatureData;
                //        //signatureCapture = new MemoryStream(HTMLValueBytes[1]);
                //        signatureCapture = new MemoryStream(prunedArray);

                //        // Convert to image and output to "C:\Temp\Signature.json"
                //        using (FileStream file = new System.IO.FileStream(Path.Combine("C:\\Temp", "Signature.json"), FileMode.Create, FileAccess.Write))
                //        {
                //            file.Write(prunedArray, 0, prunedArray.Length);
                //        }

                //    }
                //}

                Task.Run(() => device.DeviceSetIdle());
            }

            return signatureCapture;
        }

        private void DoSignatureWork(object obj)
        {
            object[] realObject = (object[])obj;
            LinkRequest linkRequest = realObject[0] as LinkRequest;
            CancellationTokenSource cancellationTokenSource = (CancellationTokenSource) realObject[1];

            bool operationCompleted = false;
            CancellationToken token = cancellationTokenSource.Token;

            token.Register(() =>
            {
                Debug.WriteLine("Task timed out !!! Stop");

                if (!operationCompleted)
                {
                    Debug.WriteLine("DEVICE RESCUE IN PROGRESS =========================================================================================");
                    linkRequest = RequestBuilder.LinkAbortCommandRequest();
                    device.AbortCommand(linkRequest);
                    resetEvent.Set();
                }
            });

            // create a new task listening the token
            Task.Run(() =>
            {
                Task.Factory.StartNew(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }

                    Debug.WriteLine("Task SIGNALLING timeout");

                }, token);

                //Stopwatch time = Stopwatch.StartNew();
                //for (int i = 0; i < 5; i++)
                //{
                //    Debug.WriteLine("run...");
                //    Thread.Sleep(1000);
                //}
                //time.Stop();
                //Debug.WriteLine("Task end. cost:{0}", time.ElapsedMilliseconds);

                device.GetSignature(linkRequest, cancellationTokenSource.Token);

                operationCompleted = true;

                resetEvent.Set();

            }, token);
        }
    }
}
