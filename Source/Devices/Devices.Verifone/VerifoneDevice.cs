using Common.LoggerManager;
using Devices.Common;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.SignatureProcessor;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Interfaces;
using Ninject;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XO.Device;
using XO.Private;
using XO.Requests;
using XO.Responses;

namespace Devices.Verifone
{
    [Export(typeof(ICardDevice))]
    [Export("Verifone-M400", typeof(ICardDevice))]
    [Export("Verifone-P200", typeof(ICardDevice))]
    [Export("Verifone-P400", typeof(ICardDevice))]
    [Export("Verifone-UX300", typeof(ICardDevice))]
    public class VerifoneDevice : BasePaymentDevice, IDisposable, ICardDevice
    {
        public override string Name => StringValueAttribute.GetStringValue(DeviceType.Verifone);

        //public event PublishEvent PublishEvent;
        //public event DeviceEventHandler DeviceEventOccured;

        private SerialConnection SerialConnection { get; set; }

        private DeviceConfig _config;

        [Inject]
        public IVIPA VipaDevice { get; set; } = new VIPAImpl();

        //public DeviceInformation DeviceInformation { get; private set; }

        public override string ManufacturerConfigID => DeviceType.Verifone.ToString();

        public override int SortOrder { get; set; } = -1;

        public VerifoneDevice()
        {

        }

        public override object Clone()
        {
            VerifoneDevice clonedObj = new VerifoneDevice();
            return clonedObj;
        }

        public override void Dispose()
        {
            VipaDevice?.Dispose();
        }

        public override void Disconnect()
        {
            SerialConnection?.Disconnect();
        }

        public override bool IsConnected(LinkRequest request)
        {
            if (request != null)
            {
                LinkActionRequest linkActionRequest = request.Actions.First();
                IVIPA device = LocateDevice(linkActionRequest?.DALRequest?.DeviceIdentifier);
                if (device.IsConnected())
                {
                    return true;
                }
            }
            return false;
        }

        public override List<LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool active)
        {
            DeviceInformation = deviceInfo;
            DeviceInformation.Manufacturer = ManufacturerConfigID;
            DeviceInformation.ComPort = deviceInfo.ComPort;

            SerialConnection = new SerialConnection(DeviceInformation, null);
            active = VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection);

            if (active)
            {
                (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    // check for power on notification: reissue reset command to obtain device information
                    if (deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification != null)
                    {
                        Console.WriteLine($"\nDEVICE EVENT: Terminal ID={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification?.TerminalID}," +
                            $" EVENT='{deviceIdentifier.deviceInfoObject.LinkDeviceResponse.PowerOnNotification?.TransactionStatusMessage}'");

                        deviceIdentifier = VipaDevice.DeviceCommandReset();

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
                    VipaDevice = VipaDevice;
                    _config = config;
                    active = true;

                    Console.WriteLine($"\nDEVICE PROBE SUCCESS ON {DeviceInformation?.ComPort}, FOR SN: {DeviceInformation?.SerialNumber}");
                }
                else
                {
                    //VipaDevice.CancelResponseHandlers();
                    Console.WriteLine($"\nDEVICE PROBE FAILED ON {DeviceInformation?.ComPort}\n");
                }
            }
            return null;
        }

        public override List<DeviceInformation> DiscoverDevices()
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

        public override void DeviceSetIdle()
        {
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: SET TO IDLE.");
            if (VipaDevice != null)
            {
                VipaDevice.DisplayMessage(VIPA.VIPAImpl.VIPADisplayMessageValue.Idle);
            }
        }

        public override bool DeviceRecovery()
        {
            Console.WriteLine($"DEVICE: ON PORT={DeviceInformation.ComPort} - DEVICE-RECOVERY");
            return false;
        }

        public List<LinkRequest> GetDeviceResponse(LinkRequest deviceInfo)
        {
            throw new NotImplementedException();
        }

        private IVIPA LocateDevice(LinkDeviceIdentifier deviceIdentifer)
        {
            // If we have single device connected to the work station
            if (deviceIdentifer == null)
            {
                return VipaDevice;
            }

            // get device serial number
            string deviceSerialNumber = DeviceInformation?.SerialNumber;

            if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                // clear up any commands the device might be processing
                //VipaDevice.AbortCurrentCommand();

                //SetDeviceVipaInfo(VipaDevice, true);
                //deviceSerialNumber = deviceVIPAInfo.deviceInfoObject?.LinkDeviceResponse?.SerialNumber;
            }

            if (!string.IsNullOrWhiteSpace(deviceSerialNumber))
            {
                // does device serial number match LinkDeviceIdentifier serial number
                if (deviceSerialNumber.Equals(deviceIdentifer.SerialNumber, StringComparison.CurrentCultureIgnoreCase))
                {
                    return VipaDevice;
                }
                else
                {
                    //VipaDevice.DisplayMessage(VIPADisplayMessageValue.Idle);
                }
            }

            return VipaDevice;
        }

        //private (HTMLResponseObject htmlResponseObject, int VipaResponse) ProcessSignatureRequest(IVIPA device, LinkRequest request, LinkActionRequest linkActionRequest, CancellationToken cancellationToken)
        //{
        //    (HTMLResponseObject htmlResponseObject, int VipaResponse) htmlResult = VipaDevice.GetSignature();

        //    // check for timeout
        //    if (cancellationToken.IsCancellationRequested)
        //    {
        //        // Reset contactless reader to hide contactless status bar if device is unplugged and replugged during a payment workflow
        //        _ = Task.Run(() => VipaDevice.CloseContactlessReader(true));
        //        //SetErrorResponse(linkActionRequest, EventCodeType.REQUEST_TIMEOUT, VipaResponse, StringValueAttribute.GetStringValue(DeviceEvent.RequestTimeout));
        //        return (null, 0);
        //    }

        //    if (htmlResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
        //    {
        //        int offset = htmlResult.htmlResponseObject.SignatureData.Count == 1 ? 0 : 1;
        //        byte[] prunedArray = SignaturePointsConverter.PruneByteArray(htmlResult.htmlResponseObject.SignatureData[offset]);

        //        // Remove end-of-stroke separator
        //        byte[] signatureImagePayload = SignaturePointsConverter.ConvertPointsToImage(prunedArray.Where(x => x != 0).ToArray());

        //        request.Actions[0].DALRequest.LinkObjects.ESignatureImage = signatureImagePayload;
        //        request.Actions[0].DALRequest.LinkObjects.MaxBytes = signatureImagePayload.Length;

        //        // note: paste the below code into VerifoneDevice::ProcessSignatureRequest method above the code that "Cleans up Memory"
        //        // Convert to image and output to "C:\Temp\Signature.json"

        //        using (System.IO.FileStream file = new System.IO.FileStream(System.IO.Path.Combine("C:\\Temp", "Signature.json"), System.IO.FileMode.Create, System.IO.FileAccess.Write))
        //        {
        //            file.Write(prunedArray, 0, prunedArray.Length);
        //        }
        //    }

        //    return htmlResult;
        //}

        private void ProcessSignatureRequest(IVIPA device, LinkRequest request, LinkActionRequest linkActionRequest, CancellationToken cancellationToken)
        {
            (LinkDALRequestIPA5Object linkActionRequestIPA5Object, int VipaResponse) = device.ProcessSignatureRequest(linkActionRequest);

            // check for timeout
            if (cancellationToken.IsCancellationRequested)
            {
                // Reset contactless reader to hide contactless status bar if device is unplugged and replugged during a payment workflow
                _ = Task.Run(() => VipaDevice.CloseContactlessReader(true));
                //SetErrorResponse(linkActionRequest, EventCodeType.REQUEST_TIMEOUT, VipaResponse, StringValueAttribute.GetStringValue(DeviceEvent.RequestTimeout));
                return;
            }

            if (VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                // allow for single signature capture
                if (linkActionRequestIPA5Object.SignatureData.Count > 0)
                {
                    request.Actions[0].DALRequest.LinkObjects.SignatureData = linkActionRequestIPA5Object.SignatureData;
                    request.Actions[0].DALRequest.LinkObjects.SignatureName = linkActionRequestIPA5Object.SignatureName;

                    int offset = linkActionRequestIPA5Object.SignatureData.Count == 1 ? 0 : 1;
                    byte[] prunedArray = SignaturePointsConverter.PruneSignaturePointsByteArray(linkActionRequestIPA5Object.SignatureData[offset]);

                    // remove signature separtor
                    //prunedArray = SignaturePointsConverter.RemoveSignatureSeparatorBytes(prunedArray);

                    Logger.debug($"{BitConverter.ToString(prunedArray, 0, prunedArray.Length).Replace("-", "")}");

                    // Remove end-of-stroke separator
                    byte[] signatureImagePayload = SignaturePointsConverter.ConvertPointsToImage(prunedArray.Where(x => x != 0).ToArray());

                    request.Actions[0].DALRequest.LinkObjects.SignatureData[0] = prunedArray;
                    request.Actions[0].DALRequest.LinkObjects.ESignatureImage = signatureImagePayload;
                    request.Actions[0].DALRequest.LinkObjects.MaxBytes = signatureImagePayload?.Length ?? 0;

                    if (request.Actions[0].DALRequest.LinkObjects.MaxBytes > 0)
                    {
                        using (System.IO.FileStream file = new System.IO.FileStream(System.IO.Path.Combine("C:\\Temp", "Signature.json"), System.IO.FileMode.Create, System.IO.FileAccess.Write))
                        {
                            file.Write(prunedArray, 0, prunedArray.Length);
                        }
                    }

                    // Clean up Memory
                    //foreach (byte[] array in request.Actions[0].DALRequest.LinkObjects.SignatureData)
                    //{
                    //    ArrayPool<byte>.Shared.Return(array, true);
                    //}

                    Logger.debug(string.Format("signature conversion: {0}", request.Actions[0].DALRequest.LinkObjects.MaxBytes > 0 ? "SUCCESS" : "FAILED"));
                }
            }
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);

                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (int VipaResult, int VipaResponse) response = VipaDevice.GetActiveKeySlot();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);

                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) response = VipaDevice.GetEMVKernelChecksum();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);

                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) config = (new SecurityConfigurationObject(), (int)VipaSW1SW2Codes.Failure);
                        config = VipaDevice.GetSecurityConfiguration(config.securityConfigurationObject.VSSPrimarySlot, config.securityConfigurationObject.ADEProductionSlot);
                        if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: FIRMARE VERSION  ={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                            Console.WriteLine($"DEVICE: ADE-{config.securityConfigurationObject.KeySlotNumber} KEY KSN   ={config.securityConfigurationObject.SRedCardKSN}");
                            config = VipaDevice.GetSecurityConfiguration(config.securityConfigurationObject.VSSPrimarySlot, config.securityConfigurationObject.ADETestSlot);
                            if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine($"DEVICE: ADE-{config.securityConfigurationObject.KeySlotNumber} KEY KSN   ={config.securityConfigurationObject.SRedCardKSN}");
                            }
                            Console.WriteLine($"DEVICE: VSS SLOT NUMBER  ={config.securityConfigurationObject.VSSPrimarySlot - 0x01}");
                            Console.WriteLine($"DEVICE: ONLINE PIN KSN   ={config.securityConfigurationObject.OnlinePinKSN}");
                            // validate configuration
                            int vipaResponse = VipaDevice.ValidateConfiguration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model);
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
                            config = VipaDevice.GetSecurityConfiguration(config.securityConfigurationObject.VSSPrimarySlot, config.securityConfigurationObject.ADETestSlot);
                            if (config.VipaResponse == (int)VipaSW1SW2Codes.Success)
                            {
                                Console.WriteLine($"DEVICE: FIRMARE VERSION  ={deviceIdentifier.deviceInfoObject.LinkDeviceResponse.FirmwareVersion}");
                                Console.WriteLine($"DEVICE: ADE-{config.securityConfigurationObject.KeySlotNumber} KEY KSN   ={config.securityConfigurationObject.SRedCardKSN}");
                                Console.WriteLine($"DEVICE: VSS SLOT NUMBER  ={config.securityConfigurationObject.VSSPrimarySlot - 0x01}");
                                Console.WriteLine($"DEVICE: ONLINE PIN KSN   ={config.securityConfigurationObject.OnlinePinKSN}");
                                // validate configuration
                                int vipaResponse = VipaDevice.ValidateConfiguration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model);
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);

                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.Configuration(deviceIdentifier.deviceInfoObject.LinkDeviceResponse.Model);
                        if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                        {
                            Console.WriteLine($"DEVICE: CONFIGURATION UPDATED SUCCESSFULLY\n");
                            Console.Write("DEVICE: RELOADING CONFIGURATION...");
                            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifierExteneded = VipaDevice.DeviceExtendedReset();

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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.FeatureEnablementToken();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.LockDeviceConfiguration0();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.LockDeviceConfiguration8();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.UnlockDeviceConfiguration();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (DevicePTID devicePTID, int VipaResponse) response = VipaDevice.DeviceReboot();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        int vipaResponse = VipaDevice.UpdateHMACKeys();
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

            if (VipaDevice != null)
            {
                VipaDevice?.Dispose();
                SerialConnection = new SerialConnection(DeviceInformation, null);


                if (VipaDevice.Connect(DeviceInformation.ComPort, SerialConnection))
                {
                    (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceIdentifier = VipaDevice.DeviceCommandReset();

                    if (deviceIdentifier.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        (string HMAC, int VipaResponse) config = VipaDevice.GenerateHMAC();
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

        public LinkRequest GetSignature(LinkRequest linkRequest, CancellationToken cancellationToken)
        {
            Console.WriteLine($"DEVICE[{DeviceInformation.ComPort}]: GET SIGNATURE.");
            if (VipaDevice != null)
            {
                LinkActionRequest linkActionRequest = linkRequest.Actions.FirstOrDefault();
                ProcessSignatureRequest(VipaDevice, linkRequest, linkActionRequest, cancellationToken);
            }
            return linkRequest;
        }

        public override bool DeviceRecoveryWithMessagePreservation()
        {
            throw new NotImplementedException();
        }

        public override int GetDeviceHealthStatus()
        {
            throw new NotImplementedException();
        }

        public override LinkRequest GetStatus(LinkRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion --- subworkflow mapping
    }
}
