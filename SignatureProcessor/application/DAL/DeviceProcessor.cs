using Devices.Common;
using Devices.Verifone;
using Devices.Verifone.VIPA;
using SignatureProcessor.devices.common;
using SignatureProcessor.devices.Verifone.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace SignatureProcessor.application.DAL
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

            foreach (var array in HTMLValueBytes)
            {
                ArrayPool<byte>.Shared.Return(array, true);
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
                ComPort = "COM11"
            };

            bool active = false;
            device?.Probe(deviceConfig, currentDeviceInformation, out active);

            if (active)
            {
                (HTMLResponseObject htmlResponseObject, int VipaResponse) htmlResponseObject = device.GetSignature();
                if (htmlResponseObject.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    // Target the second output in this case
                    if (htmlResponseObject.htmlResponseObject.HTMLValueBytes.Count >= 2)
                    {
                        HTMLValueBytes = htmlResponseObject.htmlResponseObject.HTMLValueBytes;
                        signatureCapture = new MemoryStream(HTMLValueBytes[1]);
                    }
                }
                device.DeviceSetIdle();
            }

            return signatureCapture;
        }
    }
}
