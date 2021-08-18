using Devices.Common;
using Devices.Verifone;
using Devices.Verifone.VIPA;
using SignatureProcessorApp.devices.common;
using SignatureProcessorApp.devices.Verifone.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

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
                (HTMLResponseObject htmlResponseObject, int VipaResponse) htmlResponseObject = device.GetSignature();
                if (htmlResponseObject.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    // Target the second output in this case
                    if (htmlResponseObject.htmlResponseObject?.SignatureData?.Count >= 2)
                    {
                        HTMLValueBytes = htmlResponseObject.htmlResponseObject.SignatureData;
                        signatureCapture = new MemoryStream(HTMLValueBytes[1]);
                    }
                }
                device.DeviceSetIdle();
            }

            return signatureCapture;
        }
    }
}
