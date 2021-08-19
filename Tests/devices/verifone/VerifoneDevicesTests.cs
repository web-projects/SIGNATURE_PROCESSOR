using System;
using Xunit;
using Devices.Verifone;
using Devices.Common;
using SignatureProcessorApp.devices.common;
using System.Linq;
using TestHelper;
using Devices.Verifone.VIPA;
using System.Threading;
using XO.Requests;
using Devices.Verifone.Tests.Helpers;

namespace Devices.Verifone.Tests
{
    public class VerifoneDeviceTests : IDisposable
    {
        const string skipfact = null;//"Device Only";  // set this to null to run skipable tests

        private readonly DeviceConfig deviceConfig;
        private readonly DeviceInformation currentDeviceInformation;

        private readonly VerifoneDevice subject;

        public VerifoneDeviceTests()
        {
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
            else
            {
                currentDeviceInformation = new DeviceInformation()
                {
                    Manufacturer = subject.ManufacturerConfigID,
                    Model = subject.Name,
                    SerialNumber = "DEADBEEF",
                };
            }

            subject = new VerifoneDevice();
        }

        public void Dispose()
        {
           
        }

        [Fact]
        public void VerifoneDevice_ShouldNotReturnNull_WhenTestingDALGetSignature()
        {
            Assert.Null(subject.Probe(deviceConfig, currentDeviceInformation, out bool active));
            Assert.True(active);

            IVIPA vipaDevice = Helper.GetPropertyValueFromInstance<IVIPA>("VipaDevice", true, false, subject);
            //vipaDevice.SupportedTransactions = deviceConfig.SupportedTransactions;
            Helper.SetPropertyValueToInstance<IVIPA>("VipaDevice", true, false, subject, vipaDevice);

            LinkRequest linkRequest = RequestBuilder.LinkGetSignatureRequest();

            LinkRequest linkRequestResponse = subject.GetSignature(linkRequest, It.IsAny<CancellationToken>());

            Assert.Null(linkRequestResponse.LinkObjects.LinkActionResponseList[0].Errors);
            Assert.Equal(1, linkRequestResponse.LinkObjects.LinkActionResponseList[0].DALResponse.Devices?.Count);
            Assert.NotNull(linkRequest.Actions[0].DALRequest.LinkObjects.ESignatureImage);

            subject.DeviceSetIdle();

            // note: paste the below code into VerifoneDevice::ProcessSignatureRequest method above the code that "Cleans up Memory"
            // Convert to image and output to "C:\Temp\Signature.json"
            //using (System.IO.FileStream file = new System.IO.FileStream(System.IO.Path.Combine("C:\\Temp", "Signature.json"), System.IO.FileMode.Create, System.IO.FileAccess.Write))
            //{
            //    file.Write(linkActionRequestIPA5Object.SignatureData[offset], 0, linkActionRequestIPA5Object.SignatureData[offset].Length);
            //}

            // Convert to image and output to "C:\Temp\Signature.png"
            ImageRenderer.CreateImageFromStream(linkRequest.Actions[0].DALRequest.LinkObjects.ESignatureImage);
            Assert.True(File.Exists("C:\\Temp\\Signature.png"));
        }
    }
}