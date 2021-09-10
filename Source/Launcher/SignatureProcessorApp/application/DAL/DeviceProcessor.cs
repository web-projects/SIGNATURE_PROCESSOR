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
using System.IO;
using System.Threading;
using XO.Requests;

namespace SignatureProcessorApp.application.DAL
{
    public class DeviceProcessor : IDisposable
    {
        private VerifoneDevice device;
        private List<byte[]> HTMLValueBytes;
        private MemoryStream signatureCapture;

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
                (HTMLResponseObject htmlResponseObject, int VipaResponse) htmlResponseObject = device.GetSignature(linkRequest, CancellationToken.None);

                if (htmlResponseObject.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    // Target the second output in this case
                    if (htmlResponseObject.htmlResponseObject?.SignatureData?.Count >= 2)
                    {
                        int offset = htmlResponseObject.htmlResponseObject.SignatureData.Count == 1 ? 0 : 1;
                        byte[] prunedArray = SignaturePointsConverterHelper.PruneByteArray(htmlResponseObject.htmlResponseObject.SignatureData[offset]);

                        //HTMLValueBytes = htmlResponseObject.htmlResponseObject.SignatureData;
                        //signatureCapture = new MemoryStream(HTMLValueBytes[1]);
                        signatureCapture = new MemoryStream(prunedArray);

                        // Convert to image and output to "C:\Temp\Signature.json"
                        using (System.IO.FileStream file = new System.IO.FileStream(System.IO.Path.Combine("C:\\Temp", "Signature.json"), System.IO.FileMode.Create, System.IO.FileAccess.Write))
                        {
                            file.Write(prunedArray, 0, prunedArray.Length);
                        }

                    }
                }
                device.DeviceSetIdle();
            }

            return signatureCapture;
        }
    }
}
