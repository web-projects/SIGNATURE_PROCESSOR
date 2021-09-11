using Devices.Common;
using Devices.Common.SignatureProcessor;
using Devices.SignatureProcessor;
using Devices.Verifone.Tests.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Interfaces;
using SignatureProcessorApp.devices.common;
using SignatureProcessorApp.devices.Verifone.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using TestHelper;
using XO.Requests;
using Xunit;

namespace Devices.Verifone.Tests
{
    public class VerifoneDeviceTests : IDisposable
    {
        const string skipfact = null;//"Device Only";  // set this to null to run skipable tests

        private readonly DeviceConfig deviceConfig;
        private readonly DeviceInformation currentDeviceInformation;

        private readonly VerifoneDevice subject;

        private CancellationTokenSource cancelTokenSource;

        public VerifoneDeviceTests()
        {
            subject = new VerifoneDevice();

            deviceConfig = new DeviceConfig()
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

            if (skipfact == null)
            {
                currentDeviceInformation = subject.DiscoverDevices()?.FirstOrDefault();
                // IPP Configuration
                //currentDeviceInformation.ConfigurationHostId = 0x05;
                //currentDeviceInformation.OnlinePinKeySetId = 0x01;
            }
            //else
            //{
            //    currentDeviceInformation = new DeviceInformation()
            //    {
            //        Manufacturer = subject.ManufacturerConfigID,
            //        Model = subject.Name,
            //        SerialNumber = "DEADBEEF",
            //    };
            //}
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //subject.VipaDevice?.Disconnect();

                if (cancelTokenSource != null)
                {
                    cancelTokenSource.Dispose();
                    cancelTokenSource = null;
                }
                subject.Dispose();
            }
        }

        //[Fact(Skip = skipfact)]
        [Fact]
        public void VerifoneDevice_ShouldNotReturnNull_WhenTestingDALGetSignature()
        {
            Assert.Null(subject.Probe(deviceConfig, currentDeviceInformation, out bool active));
            Assert.True(active);

            IVIPA VipaDevice = Helper.GetPropertyValueFromInstance<IVIPA>("VipaDevice", true, false, subject);
            //VipaDevice.SupportedTransactions = deviceConfig.SupportedTransactions;
            Helper.SetPropertyValueToInstance<IVIPA>("VipaDevice", true, false, subject, VipaDevice);

            LinkRequest linkRequest = RequestBuilder.LinkGetSignatureRequest();

            //LinkRequest linkRequestResponse = subject.GetSignature(linkRequest, CancellationToken.None);
            subject.GetSignature(linkRequest, CancellationToken.None);

            //(HTMLResponseObject htmlResponseObject, int VipaResponse) =

            //Assert.Null(linkRequestResponse.LinkObjects.LinkActionResponseList[0].Errors);
            //Assert.Equal(1, linkRequestResponse.LinkObjects.LinkActionResponseList[0].DALResponse.Devices?.Count);
            Assert.NotNull(linkRequest.Actions[0].DALRequest.LinkObjects.ESignatureImage);

            subject.DeviceSetIdle();

            // note: paste the below code into VerifoneDevice::ProcessSignatureRequest method above the code that "Cleans up Memory"
            // Convert to image and output to "C:\Temp\Signature.json"
            //using (System.IO.FileStream file = new System.IO.FileStream(System.IO.Path.Combine("C:\\Temp", "Signature.json"), System.IO.FileMode.Create, System.IO.FileAccess.Write))
            //{
            //    file.Write(signatureImagePayload, 0, signatureImagePayload.Length);
            //}

            // Convert to image and output to "C:\Temp\Signature.png"
            ImageRenderer.CreateImageFromStream(linkRequest.Actions[0].DALRequest.LinkObjects.ESignatureImage);
            Assert.True(File.Exists("C:\\Temp\\Signature.png"));
        }
    }
}