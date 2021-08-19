using Devices.Common;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA;
using Ninject;
using SignatureProcessorApp.devices.Verifone.Helpers;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using XO.Requests;
using XO.Responses;

namespace Devices.Verifone
{
    [Export(typeof(ICardDevice))]
    [Export("Verifone-M400", typeof(ICardDevice))]
    [Export("Verifone-P200", typeof(ICardDevice))]
    [Export("Verifone-P400", typeof(ICardDevice))]
    [Export("Verifone-UX300", typeof(ICardDevice))]
    internal class VerifoneDevice : IDisposable, ICardDevice
    {
        public string Name => StringValueAttribute.GetStringValue(DeviceType.Verifone);

        //public event PublishEvent PublishEvent;
        public event DeviceEventHandler DeviceEventOccured;

        private SerialConnection SerialConnection { get; set; }

        private bool IsConnected { get; set; }

        private DeviceConfig _config;

        [Inject]
        internal IVIPA vipaDevice { get; set; } = new VIPA.VIPA();

        public DeviceInformation DeviceInformation { get; private set; }

        public string ManufacturerConfigID => DeviceType.Verifone.ToString();

        public int SortOrder { get; set; } = -1;

        public VerifoneDevice()
        {

        }

        public object Clone()
        {
            VerifoneDevice clonedObj = new VerifoneDevice();
            return clonedObj;
        }

        public void Dispose()
        {
            vipaDevice?.Dispose();
            IsConnected = false;
        }

        public void Disconnect()
        {
            SerialConnection?.Disconnect();
            IsConnected = false;
        }

        bool ICardDevice.IsConnected(object request)
        {
            return IsConnected;
        }

        public List<LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool active)
        {
            DeviceInformation = deviceInfo;
            DeviceInformation.Manufacturer = ManufacturerConfigID;
            DeviceInformation.ComPort = deviceInfo.ComPort;

            SerialConnection = new SerialConnection(DeviceInformation);
            active = IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);

            if (active)
            {
                (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    // check for power on notification: reissue reset command to obtain device information
                    if (deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification != null)
                    {
                        Console.WriteLine($"\nDEVICE EVENT: Terminal ID={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification?.TerminalID}," +
                            $" EVENT='{deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification?.TransactionStatusMessage}'");

                        deviceIdentifier = vipaDevice.DeviceCommandReset();

                        if (deviceIdentifier.VipaResponse != (int)VipaSW1SW2Codes.Success)
                        {
                            return null;
                        }
                    }

                    if (DeviceInformation != null)
                    {
                        DeviceInformation.Manufacturer = ManufacturerConfigID;
                        DeviceInformation.Model = deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model;
                        DeviceInformation.SerialNumber = deviceIdentifier.deviceInfoObject.LinkDeviceResponse.SerialNumber;
                    }
                    vipaDevice = vipaDevice;
                    _config = config;
                    active = true;

                    Console.WriteLine($"\nDEVICE PROBE SUCCESS ON {DeviceInformation?.ComPort}, FOR SN: {DeviceInformation?.SerialNumber}");
                }
                else
                {
                    //vipaDevice.CancelResponseHandlers();
                    Console.WriteLine($"\nDEVICE PROBE FAILED ON {DeviceInformation?.ComPort}\n");
                }
            }
            return null;
        }

        public List<DeviceInformation> DiscoverDevices()
        {
            List<DeviceInformation> deviceInformation = new List<DeviceInformation>();
            Connection.DeviceDiscovery deviceDiscovery = new Connection.DeviceDiscovery();
            if (deviceDiscovery.FindVerifoneDevices())
            {
                foreach (var device in deviceDiscovery.deviceInfo)
                {
                    if (string.IsNullOrEmpty(device.ProductID) || string.IsNullOrEmpty(device.SerialNumber))
                        throw new Exception("The connected device's PID or SerialNumber did not match with the expected values!");

                    deviceInformation.Add(new DeviceInformation()
                    {
                        ComPort = device.ComPort,
                        ProductIdentification = device.ProductID,
                        SerialNumber = device.SerialNumber,
                        VendorIdentifier = Connection.DeviceDiscovery.VID
                    });

                    System.Diagnostics.Debug.WriteLine($"device: ON PORT={device.ComPort} - VERIFONE MODEL={deviceInformation[deviceInformation.Count - 1].ProductIdentification}, " +
                        $"SN=[{deviceInformation[deviceInformation.Count - 1].SerialNumber}], PORT={deviceInformation[deviceInformation.Count - 1].ComPort}");
                }
            }

            // validate COMM Port
            if (!deviceDiscovery.deviceInfo.Any() || deviceDiscovery.deviceInfo[0].ComPort == null || !deviceDiscovery.deviceInfo[0].ComPort.Any())
            {
                return null;
            }

            return deviceInformation;
        }

        public void DeviceSetIdle()
        {
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: SET TO IDLE.");
            if (vipaDevice != null)
            {
                vipaDevice.DisplayMessage(VIPA.VIPA.VIPADisplayMessageValue.Idle);
            }
        }

        public bool DeviceRecovery()
        {
            Console.WriteLine($"DEVICE: ON PORT={DeviceInformation.ComPort} - DEVICE-RECOVERY");
            return false;
        }

        public List<LinkRequest> GetDeviceResponse(LinkRequest deviceInfo)
        {
            throw new NotImplementedException();
        }

        public (HTMLResponseObject htmlResponseObject, int VipaResponse) GetSignature()
        {
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET SIGNATURE.");
            if (vipaDevice != null)
            {
                return vipaDevice.GetSignature();
            }
            return (null, (int)VipaSW1SW2Codes.Failure);
        }

        // ------------------------------------------------------------------------
        // Methods that are mapped for usage in their respective sub-workflows.
        // ------------------------------------------------------------------------
        #region --- subworkflow mapping
        public LinkRequest GetStatus(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET STATUS for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            return linkRequest;
        }

        public LinkRequest GetActiveKeySlot(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET ACTIVE SLOT for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (int VipaResult, int VipaResponse) response = vipaDevice.GetActiveKeySlot();
                        if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: VIPA ACTIVE ADE KEY SLOT={response.VipaResult}\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED GET ACTIVE SLOT REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest GetEMVKernelChecksum(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET KERNEL CHECKSUM for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) response = vipaDevice.GetEMVKernelChecksum();
                        if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            string[] kernelInformation = response.kernelConfigurationObject.ApplicationKernelInformation.SplitByLength(8).ToArray();

                            if (kernelInformation.Length == 4)
                            {
                                Console.WriteLine(string.Format("VIPA KERNEL CHECKSUM={0}-{1}-{2}-{3}",
                                   kernelInformation[0], kernelInformation[1], kernelInformation[2], kernelInformation[3]));
                            }
                            else
                            {
                                Console.WriteLine(string.Format("VIPA KERNEL CHECKSUM={0}",
                                    response.kernelConfigurationObject.ApplicationKernelInformation));
                            }

                            bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model.Substring(0, 4)));

                            if (response.kernelConfigurationObject.ApplicationKernelInformation.Substring(BinaryStatusObject.EMV_KERNEL_CHECKSUM_OFFSET).Equals(IsEngageDevice ? BinaryStatusObject.ENGAGE_EMV_KERNEL_CHECKSUM : BinaryStatusObject.UX301_EMV_KERNEL_CHECKSUM,
                                StringComparison.CurrentCultureIgnoreCase))
                            {
                                Console.WriteLine("VIPA EMV KERNEL VALIDATED");
                            }
                            else
                            {
                                Console.WriteLine("VIPA EMV KERNEL IS INVALID");
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED GET KERNEL CHECKSUM REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest GetSecurityConfiguration(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET SECURITY CONFIGURATION for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) config = (new SecurityConfigurationObject(), (int)VipaSW1SW2Codes.Failure);
                        config = vipaDevice.GetSecurityConfiguration(config.securityConfigurationObject.VSSPrimarySlot, config.securityConfigurationObject.ADEProductionSlot);
                        if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: FIRMARE VERSION  ={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                            Console.WriteLine($"DEVICE: ADE-{config.securityConfigurationObject.KeySlotNumber} KEY KSN   ={config.securityConfigurationObject.SRedCardKSN}");
                            config = vipaDevice.GetSecurityConfiguration(config.securityConfigurationObject.VSSPrimarySlot, config.securityConfigurationObject.ADETestSlot);
                            if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine($"DEVICE: ADE-{config.securityConfigurationObject.KeySlotNumber} KEY KSN   ={config.securityConfigurationObject.SRedCardKSN}");
                            }
                            Console.WriteLine($"DEVICE: VSS SLOT NUMBER  ={config.securityConfigurationObject.VSSPrimarySlot - 0x01}");
                            Console.WriteLine($"DEVICE: ONLINE PIN KSN   ={config.securityConfigurationObject.OnlinePinKSN}");
                            // validate configuration
                            int vipaResponse = vipaDevice.ValidateConfiguration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model);
                            if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine($"DEVICE: CONFIGURATION IS VALID\n");
                            }
                            else
                            {
                                Console.WriteLine(string.Format("DEVICE: CONFIGURATION VALIDATION FAILED WITH ERROR=0x{0:X4}\n", vipaResponse));
                            }
                            Console.WriteLine("");
                        }
                        else
                        {
                            config = vipaDevice.GetSecurityConfiguration(config.securityConfigurationObject.VSSPrimarySlot, config.securityConfigurationObject.ADETestSlot);
                            if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine($"DEVICE: FIRMARE VERSION  ={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                                Console.WriteLine($"DEVICE: ADE-{config.securityConfigurationObject.KeySlotNumber} KEY KSN   ={config.securityConfigurationObject.SRedCardKSN}");
                                Console.WriteLine($"DEVICE: VSS SLOT NUMBER  ={config.securityConfigurationObject.VSSPrimarySlot - 0x01}");
                                Console.WriteLine($"DEVICE: ONLINE PIN KSN   ={config.securityConfigurationObject.OnlinePinKSN}");
                                // validate configuration
                                int vipaResponse = vipaDevice.ValidateConfiguration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model);
                                if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                                {
                                    Console.WriteLine($"DEVICE: CONFIGURATION IS VALID\n");
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("DEVICE: CONFIGURATION VALIDATION FAILED WITH ERROR=0x{0:X4}\n", vipaResponse));
                                }
                                Console.WriteLine("");
                            }
                        }
                        DeviceSetIdle();
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest Configuration(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: CONFIGURATION for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = vipaDevice.Configuration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model);
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION UPDATED SUCCESSFULLY\n");
                            Console.Write("DEVICE: RELOADING CONFIGURATION...");
                            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = vipaDevice.DeviceExtendedReset();

                            if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine("SUCCESS!");
                            }
                            else
                            {
                                Console.WriteLine("FAILURE - PLEASE REBOOT DEVICE!");
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest FeatureEnablementToken(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: FEATURE ENABLEMENT TOKEN for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = vipaDevice.FeatureEnablementToken();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: FET UPDATED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED FET REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest LockDeviceConfiguration0(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: LOCK DEVICE CONFIGURATION 0 for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = vipaDevice.LockDeviceConfiguration0();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION LOCKED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED LOCK CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest LockDeviceConfiguration8(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: LOCK DEVICE CONFIGURATION 8 for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = vipaDevice.LockDeviceConfiguration8();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION LOCKED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED LOCK CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest UnlockDeviceConfiguration(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: UNLOCK DEVICE CONFIGURATION for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = vipaDevice.UnlockDeviceConfiguration();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION UNLOCKED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED UNLOCK CONFIGURATION REQUEST WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest AbortCommand(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE: ABORT COMMAND for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            return linkRequest;
        }

        public LinkRequest ResetDevice(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE: RESET DEVICE for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");
            return linkRequest;
        }

        public LinkRequest RebootDevice(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: REBOOT DEVICE with SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (DevicePTID devicePTID, int VipaResponse) response = vipaDevice.DeviceReboot();
                        if (response.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            //Console.WriteLine($"DEVICE: REBOOT SUCCESSFULLY for ID={response.devicePTID.PTID}, SN={response.devicePTID.SerialNumber}\n");
                            Console.WriteLine($"DEVICE: REBOOT REQUEST RECEIVED SUCCESSFULLY");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED REBOOT REQUEST WITH ERROR=0x{0:X4}\n", response.VipaResponse));
                        }
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest UpdateHMACKeys(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: UPDATE HMAC KEYS for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = vipaDevice.UpdateHMACKeys();
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: HMAC KEYS UPDATED SUCCESSFULLY\n");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: FAILED HMAC KEYS UPDATE WITH ERROR=0x{0:X4}\n", vipaResponse));
                        }
                        DeviceSetIdle();
                    }
                }
            }

            return linkRequest;
        }

        public LinkRequest GenerateHMAC(LinkRequest linkRequest)
        {
            LinkActionRequest linkActionRequest = linkRequest?.Actions?.First();
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GENERATE HMAC for SN='{linkActionRequest?.DeviceRequest?.DeviceIdentifier?.SerialNumber}'");

            if (vipaDevice != null)
            {
                if (!IsConnected)
                {
                    vipaDevice.Dispose();
                    SerialConnection = new SerialConnection(DeviceInformation);
                    IsConnected = vipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);
                }

                if (IsConnected)
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = vipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (string HMAC, int VipaResponse) config = vipaDevice.GenerateHMAC();
                        if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: HMAC={config.HMAC}\n");
                        }
                        DeviceSetIdle();
                    }
                }
            }

            return linkRequest;
        }

        #endregion --- subworkflow mapping
    }
}
