using Config.Config;
using Devices.Common.Helpers;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.TLV;
using Devices.Verifone.VIPA.Templates;
using SignatureProcessor.devices.Verifone.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XO.Private;
using XO.Responses;
using static Devices.Verifone.Helpers.Messages;

namespace Devices.Verifone.VIPA
{
    public class VIPA : IVIPA, IDisposable
    {
        #region --- enumerations ---
        public enum VIPADisplayMessageValue
        {
            Custom = 0x00,
            Idle = 0x01,
            ProcessingTransaction = 0x02,
            Authorising = 0x03,
            RequestRejected = 0x04,
            InsertCardWithBeeps = 0x0D,
            RemoveCardWithBeeps = 0x0E,
            Processing = 0x0F
        }
        #endregion --- enumerations ---

        #region --- attributes ---
        private enum ResetDeviceCfg
        {
            ReturnSerialNumber = 1 << 0,
            ReturnAfterCardRemoval = 1 << 1,
            LeaveScreenDisplayUnchanged = 1 << 2,
            SlideShowStartsNormalTiming = 1 << 3,
            NoBeepDuringReset = 1 << 4,
            ResetImmediately = 1 << 5,
            ReturnPinpadConfiguration = 1 << 6,
            AddVOSComponentsInformation = 1 << 7
        }

        // Optimal Packet Size for READ/WRITE operations on device
        const int PACKET_SIZE = 1024;

        private int ResponseTagsHandlerSubscribed = 0;

        public TaskCompletionSource<int> ResponseCodeResult = null;

        public delegate void ResponseTagsHandlerDelegate(List<TLV.TLV> tags, int responseCode, bool cancelled = false);
        internal ResponseTagsHandlerDelegate ResponseTagsHandler = null;

        public delegate void ResponseTaglessHandlerDelegate(byte[] data, int responseCode, bool cancelled = false);
        internal ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;

        public delegate void ResponseCLessHandlerDelegate(List<TLV.TLV> tags, int responseCode, int pcb, bool cancelled = false);
        internal ResponseCLessHandlerDelegate ResponseCLessHandler = null;

        public TaskCompletionSource<(DevicePTID devicePTID, int VipaResponse)> DeviceResetConfiguration = null;

        public TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)> DeviceIdentifier = null;
        public TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)> DeviceSecurityConfiguration = null;
        public TaskCompletionSource<(KernelConfigurationObject kernelConfigurationObject, int VipaResponse)> DeviceKernelConfiguration = null;

        public TaskCompletionSource<(string HMAC, int VipaResponse)> DeviceGenerateHMAC = null;
        public TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)> DeviceBinaryStatusInformation = null;

        public TaskCompletionSource<(HTMLResponseObject htmlResponseObject, int VipaResponse)> DeviceHTMLResponse = null;

        #endregion --- attributes ---

        #region --- connection ---
        private SerialConnection serialConnection { get; set; }

        public bool Connect(string comPort, SerialConnection connection)
        {
            serialConnection = connection;
            return serialConnection.Connect(comPort);
        }

        public void Dispose()
        {
            serialConnection?.Dispose();
        }

        #endregion --- connection ---

        #region --- resources ---
        private bool FindEmbeddedResourceByName(string fileName, string fileTarget)
        {
            bool result = false;

            // Main Assembly contains embedded resources
            Assembly mainAssembly = Assembly.GetEntryAssembly();
            foreach (string name in mainAssembly.GetManifestResourceNames())
            {
                if (name.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    using (Stream stream = mainAssembly.GetManifestResourceStream(name))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        // always create working file
                        FileStream fs = File.Open(fileTarget, FileMode.Create);
                        BinaryWriter bw = new BinaryWriter(fs);
                        byte[] ba = new byte[stream.Length];
                        stream.Read(ba, 0, ba.Length);
                        bw.Write(ba);
                        br.Close();
                        bw.Close();
                        stream.Close();
                        result = true;
                    }
                    break;

                }
            }
            return result;
        }

        #endregion --- resources ---

        private void WriteSingleCmd(VIPACommand command)
        {
            serialConnection?.WriteSingleCmd(new VIPAResponseHandlers
            {
                responsetagshandler = ResponseTagsHandler,
                responsetaglesshandler = ResponseTaglessHandler,
                responsecontactlesshandler = ResponseCLessHandler
            }, command);
        }

        private void WriteRawBytes(byte[] buffer)
        {
            serialConnection?.WriteRaw(buffer);
        }

        #region --- VIPA commands ---
        public bool DisplayMessage(VIPADisplayMessageValue displayMessageValue = VIPADisplayMessageValue.Idle, bool enableBacklight = false, string customMessage = "")
        {
            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD2, ins = 0x01, p1 = (byte)displayMessageValue, p2 = (byte)(enableBacklight ? 0x01 : 0x00), data = Encoding.ASCII.GetBytes(customMessage) };
            WriteSingleCmd(command);   // Display [D2, 01]

            var displayCommandResponseCode = ResponseCodeResult.Task.Result;

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return displayCommandResponseCode == (int)VipaSW1SW2Codes.Success;
        }

        internal (int VipaData, int VipaResponse) DeviceCommandAbort()
        {
            (int VipaData, int VipaResponse) deviceResponse = (-1, (int)VipaSW1SW2Codes.Failure);

            ResponseCodeResult = new TaskCompletionSource<int>();

            DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);
            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            Debug.WriteLine(ConsoleMessages.AbortCommand.GetStringValue());
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0xFF, p1 = 0x00, p2 = 0x00 };
            WriteSingleCmd(command);

            deviceResponse = ((int)VipaSW1SW2Codes.Success, ResponseCodeResult.Task.Result);

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceResponse;
        }

        public (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceCommandReset()
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetDeviceInfoResponseHandler;

                Debug.WriteLine(ConsoleMessages.DeviceReset.GetStringValue());
                VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x00, p1 = 0x00, p2 = (byte)(ResetDeviceCfg.ReturnSerialNumber | ResetDeviceCfg.ReturnAfterCardRemoval | ResetDeviceCfg.ReturnPinpadConfiguration) };
                WriteSingleCmd(command);   // Device Info [D0, 00]

                deviceResponse = DeviceIdentifier.Task.Result;

                ResponseTagsHandler -= GetDeviceInfoResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceResponse;
        }

        public (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceExtendedReset()
        {
            (DeviceInfoObject deviceInfoObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                DeviceIdentifier = new TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetDeviceInfoResponseHandler;

                // Bit  1 – 0 PTID in serial response
                //        – 1 PTID plus serial number (tag 9F1E) in serial response
                //        - The following flags are only taken into account when P1 = 0x00:
                // Bit  2 - 0 — Leave screen display unchanged, 1 — Clear screen display to idle display state
                // Bit  3 - 0 — Slide show starts with normal timing, 1 — Start Slide-Show as soon as possible
                // Bit  4 - 0 — No beep, 1 — Beep during reset as audio indicator
                // Bit  5 - 0 — ‘Immediate’ reset, 1 — Card Removal delayed reset
                // Bit  6 - 1 — Do not add any information in the response, except serial number if Bit 1 is set.
                // Bit  7 - 0 — Do not return PinPad configuration, 1 — return PinPad configuration (warning: it can take a few seconds)
                // Bit  8 - 1 — Add V/OS components information (Vault, OpenProtocol, OS_SRED, AppManager) to
                // response (V/OS only).
                // Bit  9 – 1 - Force contact EMV configuration reload
                // Bit 10 – 1 – Force contactless EMV configuration reload
                // Bit 11 – 1 – Force contactless CAPK reload
                // Bit 12 – 1 – Returns OS components version (requires OS supporting this feature)
                // Bit 13 - 1 - Return communication mode (tag DFA21F) (0 - SERIAL, 1 - TCPIP, 3 - USB, 4 - BT, 5
                //            - PIPE_INTERNAL, 6 - WIFI, 7 - GPRS)
                // Bit 14 - 1 - Connect to external pinpad (PP1000SEV3) and set EXTERNAL_PINPAD to ON
                // Bit 15 - 1 - Disconnect external pinpad (PP1000SEV3) and set EXTERNAL_PINPAD to OFF
                var dataForReset = new List<TLV.TLV>
                {
                    new TLV.TLV
                    {
                        Tag = new byte[] { 0xE0 },
                        InnerTags = new List<TLV.TLV>
                        {
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0xDF, 0xED, 0x0D },
                                Data = new byte[] { 0x02, 0x0F }
                            }
                        }
                    }
                };
                TLV.TLV tlv = new TLV.TLV();
                byte[] dataForResetData = tlv.Encode(dataForReset);

                Debug.WriteLine(ConsoleMessages.DeviceExtendedReset.GetStringValue());
                VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x0A, p1 = 0x00, p2 = 0x00, data = dataForResetData };
                WriteSingleCmd(command);   // Device Info [D0, 00]

                deviceResponse = DeviceIdentifier.Task.Result;

                ResponseTagsHandler -= GetDeviceInfoResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceResponse;
        }

        private (DevicePTID devicePTID, int VipaResponse) DeviceRebootWithResponse()
        {
            (DevicePTID devicePTID, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);
            DeviceResetConfiguration = new TaskCompletionSource<(DevicePTID devicePTID, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += DeviceResetResponseHandler;

            Debug.WriteLine(ConsoleMessages.RebootDevice.GetStringValue());
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x00, p1 = 0x01, p2 = 0x03 };
            WriteSingleCmd(command);

            deviceResponse = DeviceResetConfiguration.Task.Result;

            ResponseTagsHandler -= DeviceResetResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceResponse;
        }

        private (DevicePTID devicePTID, int VipaResponse) DeviceRebootWithoutResponse()
        {
            (DevicePTID devicePTID, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            Debug.WriteLine(ConsoleMessages.RebootDevice.GetStringValue());
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x00, p1 = 0x01, p2 = 0x00 };
            WriteSingleCmd(command);

            ResponseCodeResult = new TaskCompletionSource<int>();

            deviceResponse = (null, (int)VipaSW1SW2Codes.Success);

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceResponse;
        }

        public (DevicePTID devicePTID, int VipaResponse) DeviceReboot()
        {
            return DeviceRebootWithoutResponse();
        }

        public (int VipaResult, int VipaResponse) GetActiveKeySlot()
        {
            // check for access to the file
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = GetBinaryStatus(BinaryStatusObject.MAPP_SRED_CONFIG);

            // When the file cannot be accessed, VIPA returns SW1SW2 equal to 9F13
            if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                    BinaryStatusObject.MAPP_SRED_CONFIG, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));
                return (-1, fileStatus.VipaResponse);
            }

            // Setup for FILE OPERATIONS
            fileStatus = SelectFileForOps(BinaryStatusObject.MAPP_SRED_CONFIG);
            if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                    BinaryStatusObject.MAPP_SRED_CONFIG, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));
                return (-1, fileStatus.VipaResponse);
            }

            // Read File Contents at OFFSET 242
            fileStatus = ReadBinaryDataFromSelectedFile(0xF2, 0x0A);
            if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
            {
                Console.WriteLine(string.Format("VIPA {0} ACCESS ERROR=0x{1:X4} - '{2}'",
                    BinaryStatusObject.MAPP_SRED_CONFIG, fileStatus.VipaResponse, ((VipaSW1SW2Codes)fileStatus.VipaResponse).GetStringValue()));

                // Clean up pool allocation, clearing the array
                if (fileStatus.binaryStatusObject.ReadResponseBytes != null)
                {
                    ArrayPool<byte>.Shared.Return(fileStatus.binaryStatusObject.ReadResponseBytes, true);
                }

                return (-1, fileStatus.VipaResponse);
            }

            (int VipaResult, int VipaResponse) response = (-1, (int)VipaSW1SW2Codes.Success);

            // Obtain SLOT number
            string slotReported = Encoding.UTF8.GetString(fileStatus.binaryStatusObject.ReadResponseBytes);
            MatchCollection match = Regex.Matches(slotReported, "slot=[0-9]", RegexOptions.Compiled);
            if (match.Count == 1)
            {
                string[] result = match[0].Value.Split('=');
                if (result.Length == 2)
                {
                    response.VipaResult = Convert.ToInt32(result[1]);
                }
            }

            // Clean up pool allocation, clearing the array
            ArrayPool<byte>.Shared.Return(fileStatus.binaryStatusObject.ReadResponseBytes, true);

            return response;
        }

        public (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) GetEMVKernelChecksum()
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetKernelInformationResponseHandler;

            DeviceKernelConfiguration = new TaskCompletionSource<(KernelConfigurationObject kernelConfigurationObject, int VipaResponse)>();

            var aidRequestedTransaction = new List<TLV.TLV>
            {
                new TLV.TLV
                {
                    Tag = new byte[] { 0xE0 },
                    InnerTags = new List<TLV.TLV>
                    {
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0x9F, 0x06, 0x0E },      // AID A000000003101001
                            Data = new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x10, 0x10 }
                        }
                    }
                }
            };
            TLV.TLV tlv = new TLV.TLV();
            var aidRequestedTransactionData = tlv.Encode(aidRequestedTransaction);

            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xDE, ins = 0x01, p1 = 0x00, p2 = 0x00, data = aidRequestedTransactionData };
            WriteSingleCmd(command);

            var deviceKernelConfigurationInfo = DeviceKernelConfiguration.Task.Result;

            ResponseTagsHandler -= GetKernelInformationResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceKernelConfigurationInfo;
        }

        public (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) GetSecurityConfiguration(byte vssSlot, byte hostID)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetSecurityInformationResponseHandler;

            DeviceSecurityConfiguration = new TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)>();

            System.Diagnostics.Debug.WriteLine(ConsoleMessages.GetSecurityConfiguration.GetStringValue());
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x11, p1 = vssSlot, p2 = hostID };
            WriteSingleCmd(command);

            var deviceSecurityConfigurationInfo = DeviceSecurityConfiguration.Task.Result;

            ResponseTagsHandler -= GetSecurityInformationResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceSecurityConfigurationInfo;
        }

        public int Configuration(string deviceModel)
        {
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = (null, (int)VipaSW1SW2Codes.Failure);

            Debug.WriteLine(ConsoleMessages.UpdateDeviceUpdate.GetStringValue());

            bool IsEngageDevice = BinaryStatusObject.ENGAGE_DEVICES.Any(x => x.Contains(deviceModel.Substring(0, 4)));

            foreach (var configFile in BinaryStatusObject.binaryStatus)
            {
                // search for partial matches in P200 vs P200Plus
                if (configFile.Value.deviceTypes.Any(x => x.Contains(deviceModel.Substring(0, 4))))
                {
                    string fileName = configFile.Value.fileName;
                    if (BinaryStatusObject.EMV_CONFIG_FILES.Any(x => x.Contains(configFile.Value.fileName)))
                    {
                        fileName = (IsEngageDevice ? "ENGAGE." : "UX301.") + configFile.Value.fileName;
                    }

                    string targetFile = Path.Combine(Constants.TargetDirectory, configFile.Value.fileName);
                    if (FindEmbeddedResourceByName(fileName, targetFile))
                    {
                        fileStatus = PutFile(configFile.Value.fileName, targetFile);
                        if (fileStatus.VipaResponse == (int)VipaSW1SW2Codes.Success && fileStatus.binaryStatusObject != null)
                        {
                            if (fileStatus.VipaResponse == (int)VipaSW1SW2Codes.Success && fileStatus.binaryStatusObject != null)
                            {
                                if (fileStatus.binaryStatusObject.FileSize == configFile.Value.fileSize)
                                {
                                    string formattedStr = string.Format("VIPA: '{0}' SIZE MATCH", configFile.Value.fileName.PadRight(13));
                                    //Console.WriteLine(formattedStr);
                                    Console.Write(string.Format("VIPA: '{0}' SIZE MATCH", configFile.Value.fileName.PadRight(13)));
                                }
                                else
                                {
                                    Console.WriteLine($"VIPA: {configFile.Value.fileName} SIZE MISMATCH!");
                                }

                                if (fileStatus.binaryStatusObject.FileCheckSum.Equals(configFile.Value.fileHash, StringComparison.OrdinalIgnoreCase) ||
                                    fileStatus.binaryStatusObject.FileCheckSum.Equals(configFile.Value.reBooted.hash, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine(", HASH MATCH");
                                }
                                else
                                {
                                    Console.WriteLine($", HASH MISMATCH!");
                                }
                            }
                        }
                        else
                        {
                            string formattedStr = string.Format("VIPA: FILE '{0}' FAILED TRANSFERRED WITH ERROR=0x{1:X4}",
                                configFile.Value.fileName.PadRight(13), fileStatus.VipaResponse);
                            Console.WriteLine(formattedStr);
                        }
                        // clean up
                        if (File.Exists(targetFile))
                        {
                            File.Delete(targetFile);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: RESOURCE '{configFile.Value.fileName}' NOT FOUND!");
                    }
                }
            }

            return fileStatus.VipaResponse;
        }

        public int ValidateConfiguration(string deviceModel)
        {
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = (null, (int)VipaSW1SW2Codes.Failure);

            foreach (var configFile in BinaryStatusObject.binaryStatus)
            {
                // search for partial matches in P200 vs P200Plus
                if (configFile.Value.deviceTypes.Any(x => x.Contains(deviceModel.Substring(0, 4))))
                {
                    fileStatus = GetBinaryStatus(configFile.Value.fileName);
                    Debug.WriteLine($"VIPA: RESOURCE '{configFile.Value.fileName}' STATUS=0x{string.Format("{0:X4}", fileStatus.VipaResponse)}");
                    if (fileStatus.VipaResponse != (int)VipaSW1SW2Codes.Success)
                    {
                        break;
                    }
                    // 20201012 - ONLY CHECK FOR FILE PRESENCE
                    Debug.WriteLine("FILE FOUND !!!");
                    // FILE SIZE
                    //if (fileStatus.binaryStatusObject.FileSize == configFile.Value.fileSize ||
                    //    fileStatus.binaryStatusObject.FileSize == configFile.Value.reBooted.size)
                    //{
                    //    string formattedStr = string.Format("VIPA: '{0}' SIZE MATCH", configFile.Value.fileName.PadRight(13));
                    //    Debug.Write(string.Format("VIPA: '{0}' SIZE MATCH", configFile.Value.fileName.PadRight(13)));
                    //}
                    //else
                    //{
                    //    Debug.WriteLine($"VIPA: {configFile.Value.fileName} SIZE MISMATCH!");
                    //    fileStatus.VipaResponse = (int)VipaSW1SW2Codes.Failure;
                    //    break;
                    //}
                    //// HASH
                    //if (fileStatus.binaryStatusObject.FileCheckSum.Equals(configFile.Value.fileHash, StringComparison.OrdinalIgnoreCase) ||
                    //    fileStatus.binaryStatusObject.FileCheckSum.Equals(configFile.Value.reBooted.hash, StringComparison.OrdinalIgnoreCase))
                    //{
                    //    Debug.WriteLine(", HASH MATCH");
                    //}
                    //else
                    //{
                    //    Debug.WriteLine($", HASH MISMATCH!");
                    //    fileStatus.VipaResponse = (int)VipaSW1SW2Codes.Failure;
                    //    break;
                    //}
                }
            }
            return fileStatus.VipaResponse;
        }

        public int FeatureEnablementToken()
        {
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = (null, (int)VipaSW1SW2Codes.Failure);
            Debug.WriteLine(ConsoleMessages.UpdateDeviceUpdate.GetStringValue());
            string targetFile = Path.Combine(Constants.TargetDirectory, BinaryStatusObject.FET_BUNDLE);
            if (FindEmbeddedResourceByName(BinaryStatusObject.FET_BUNDLE, targetFile))
            {
                fileStatus = PutFile(BinaryStatusObject.FET_BUNDLE, targetFile);
                if (fileStatus.VipaResponse == (int)VipaSW1SW2Codes.Success && fileStatus.binaryStatusObject != null)
                {
                    if (fileStatus.binaryStatusObject.FileSize == BinaryStatusObject.FET_SIZE)
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.FET_BUNDLE} SIZE MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.FET_BUNDLE} SIZE MISMATCH!");
                    }

                    if (fileStatus.binaryStatusObject.FileCheckSum.Equals(BinaryStatusObject.FET_HASH, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.FET_BUNDLE} HASH MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.FET_BUNDLE} HASH MISMATCH!");
                    }
                }
                // clean up
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
            }
            else
            {
                Console.WriteLine($"VIPA: RESOURCE '{BinaryStatusObject.FET_BUNDLE}' NOT FOUND!");
            }
            return fileStatus.VipaResponse;
        }

        public int LockDeviceConfiguration0()
        {
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = (null, (int)VipaSW1SW2Codes.Failure);
            Debug.WriteLine(ConsoleMessages.LockDeviceUpdate.GetStringValue());
            string targetFile = Path.Combine(Constants.TargetDirectory, BinaryStatusObject.LOCK_CONFIG0_BUNDLE);
            if (FindEmbeddedResourceByName(BinaryStatusObject.LOCK_CONFIG0_BUNDLE, targetFile))
            {
                fileStatus = PutFile(BinaryStatusObject.LOCK_CONFIG0_BUNDLE, targetFile);
                if (fileStatus.VipaResponse == (int)VipaSW1SW2Codes.Success && fileStatus.binaryStatusObject != null)
                {
                    if (fileStatus.binaryStatusObject.FileSize == BinaryStatusObject.LOCK_CONFIG0_SIZE)
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG0_BUNDLE} SIZE MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG0_BUNDLE} SIZE MISMATCH!");
                    }

                    if (fileStatus.binaryStatusObject.FileCheckSum.Equals(BinaryStatusObject.LOCK_CONFIG0_HASH, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG0_BUNDLE} HASH MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG0_BUNDLE} HASH MISMATCH!");
                    }
                }
                // clean up
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
            }
            else
            {
                Console.WriteLine($"VIPA: RESOURCE '{BinaryStatusObject.LOCK_CONFIG0_BUNDLE}' NOT FOUND!");
            }
            return fileStatus.VipaResponse;
        }

        public int LockDeviceConfiguration8()
        {
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = (null, (int)VipaSW1SW2Codes.Failure);
            Debug.WriteLine(ConsoleMessages.LockDeviceUpdate.GetStringValue());
            string targetFile = Path.Combine(Constants.TargetDirectory, BinaryStatusObject.LOCK_CONFIG8_BUNDLE);
            if (FindEmbeddedResourceByName(BinaryStatusObject.LOCK_CONFIG8_BUNDLE, targetFile))
            {
                fileStatus = PutFile(BinaryStatusObject.LOCK_CONFIG8_BUNDLE, targetFile);
                if (fileStatus.VipaResponse == (int)VipaSW1SW2Codes.Success && fileStatus.binaryStatusObject != null)
                {
                    if (fileStatus.binaryStatusObject.FileSize == BinaryStatusObject.LOCK_CONFIG8_SIZE)
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG8_BUNDLE} SIZE MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG8_BUNDLE} SIZE MISMATCH!");
                    }

                    if (fileStatus.binaryStatusObject.FileCheckSum.Equals(BinaryStatusObject.LOCK_CONFIG8_HASH, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG8_BUNDLE} HASH MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.LOCK_CONFIG8_BUNDLE} HASH MISMATCH!");
                    }
                }
                // clean up
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }
            }
            else
            {
                Console.WriteLine($"VIPA: RESOURCE '{BinaryStatusObject.LOCK_CONFIG8_BUNDLE}' NOT FOUND!");
            }
            return fileStatus.VipaResponse;
        }

        public int UnlockDeviceConfiguration()
        {
            (BinaryStatusObject binaryStatusObject, int VipaResponse) fileStatus = (null, (int)VipaSW1SW2Codes.Failure);
            Debug.WriteLine(ConsoleMessages.UnlockDeviceUpdate.GetStringValue());
            string targetFile = Path.Combine(Constants.TargetDirectory, BinaryStatusObject.UNLOCK_CONFIG_BUNDLE);
            if (FindEmbeddedResourceByName(BinaryStatusObject.UNLOCK_CONFIG_BUNDLE, targetFile))
            {
                fileStatus = PutFile(BinaryStatusObject.UNLOCK_CONFIG_BUNDLE, targetFile);
                if (fileStatus.VipaResponse == (int)VipaSW1SW2Codes.Success && fileStatus.binaryStatusObject != null)
                {
                    if (fileStatus.binaryStatusObject.FileSize == BinaryStatusObject.UNLOCK_CONFIG_SIZE)
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.UNLOCK_CONFIG_BUNDLE} SIZE MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.UNLOCK_CONFIG_BUNDLE} SIZE MISMATCH!");
                    }

                    if (fileStatus.binaryStatusObject.FileCheckSum.Equals(BinaryStatusObject.UNLOCK_CONFIG_HASH, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.UNLOCK_CONFIG_BUNDLE} HASH MATCH");
                    }
                    else
                    {
                        Console.WriteLine($"VIPA: {BinaryStatusObject.UNLOCK_CONFIG_BUNDLE} HASH MISMATCH!");
                    }
                }
            }
            else
            {
                Console.WriteLine($"VIPA: RESOURCE '{BinaryStatusObject.UNLOCK_CONFIG_BUNDLE}' NOT FOUND!");
            }
            return fileStatus.VipaResponse;
        }

        public (string HMAC, int VipaResponse) GenerateHMAC()
        {
            CancelResponseHandlers();

            (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) securityConfig = (new SecurityConfigurationObject(), 0);

            // HostId 06
            securityConfig = GetGeneratedHMAC(securityConfig.securityConfigurationObject.PrimarySlot,
                            HMACHasher.DecryptHMAC(Encoding.ASCII.GetString(HMACValidator.MACPrimaryPANSalt), HMACValidator.MACSecondaryKeyHASH));

            if (securityConfig.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                if (securityConfig.securityConfigurationObject.GeneratedHMAC.Equals(HMACHasher.DecryptHMAC(Encoding.ASCII.GetString(HMACValidator.MACPrimaryHASHSalt), HMACValidator.MACSecondaryKeyHASH),
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    // HostId 07
                    securityConfig = GetGeneratedHMAC(securityConfig.securityConfigurationObject.SecondarySlot, securityConfig.securityConfigurationObject.GeneratedHMAC);
                    if (securityConfig.VipaResponse == (int)VipaSW1SW2Codes.Success)
                    {
                        if (securityConfig.securityConfigurationObject.GeneratedHMAC.Equals(HMACHasher.DecryptHMAC(Encoding.ASCII.GetString(HMACValidator.MACSecondaryHASHSalt), HMACValidator.MACPrimaryKeyHASH),
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            Console.WriteLine("DEVICE: HMAC IS VALID +++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("DEVICE: HMAC SECONDARY SLOT MISMATCH=0x{0:X}", securityConfig.securityConfigurationObject.GeneratedHMAC));
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("DEVICE: HMAC PRIMARY SLOT MISMATCH=0x{0:X}", securityConfig.securityConfigurationObject.GeneratedHMAC));
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("DEVICE: HMAC PRIMARY SLOT MISMATCH=0x{0:X}", securityConfig.securityConfigurationObject.GeneratedHMAC));
                }
            }
            else
            {
                Console.WriteLine(string.Format("DEVICE: HMAC GENERATIN FAILED WITH ERROR=0x{0:X}", securityConfig.VipaResponse));
            }

            return (securityConfig.securityConfigurationObject?.GeneratedHMAC, securityConfig.VipaResponse);
        }

        private (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) GetGeneratedHMAC(int hostID, string MAC)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetGeneratedHMACResponseHandler;

            DeviceSecurityConfiguration = new TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)>();

            var dataForHMAC = new List<TLV.TLV>
            {
                new TLV.TLV
                {
                    Tag = new byte[] { 0xE0 },
                    InnerTags = new List<TLV.TLV>
                    {
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x0E },
                            Data = ConversionHelper.HexToByteArray(MAC)
                        },
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x23 },
                            Data = new byte[] { Convert.ToByte(hostID) }
                        }
                    }
                }
            };
            TLV.TLV tlv = new TLV.TLV();
            var dataForHMACData = tlv.Encode(dataForHMAC);

            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x22, p1 = 0x00, p2 = 0x00, data = dataForHMACData };
            WriteSingleCmd(command);

            var deviceSecurityConfigurationInfo = DeviceSecurityConfiguration.Task.Result;

            ResponseTagsHandler -= GetGeneratedHMACResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceSecurityConfigurationInfo;
        }

        public int UpdateHMACKeys()
        {
            string generatedHMAC = GetCurrentKSNHMAC();

            // KEY 06 Generation
            byte[] hmac_generated_key = ConversionHelper.HexToByteArray(generatedHMAC);

            // Signature = HMAC_old(old XOR new) - array1 is smaller or equal in size as array2
            byte[] hmac_signature_06 = ConversionHelper.XORArrays(hmac_generated_key, HMACValidator.HMACKEY06);

            var dataKey06HMAC = FormatE0Tag(HMACValidator.HMACKEY06, hmac_signature_06);
            TLV.TLV tlv = new TLV.TLV();
            byte[] dataForHMACData = tlv.Encode(dataKey06HMAC);

            // key slot 06
            int vipaResponse = UpdateHMACKey(0x06, dataForHMACData);

            if (vipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                // KEY 07 Generation
                byte[] hmac_signature_07 = ConversionHelper.XORArrays(hmac_generated_key, HMACValidator.HMACKEY07);

                var dataKey07HMAC = FormatE0Tag(HMACValidator.HMACKEY07, hmac_signature_07);
                tlv = new TLV.TLV();
                dataForHMACData = tlv.Encode(dataKey07HMAC);

                // key slot 07
                vipaResponse = UpdateHMACKey(0x07, dataForHMACData);
            }

            return vipaResponse;
        }

        public (HTMLResponseObject htmlResponseObject, int VipaResponse) GetSignature()
        {
            (HTMLResponseObject HTMLResponseObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                DeviceHTMLResponse = new TaskCompletionSource<(HTMLResponseObject HTMLResponseObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetSignatureResponseHandler;

                string signatureFile = "mapp/signature.html";
                string [] signatureMessage = { "please_sign_text", "ENTER SIGNATURE" };
                string [] signatureLogo = { "logo_image", "signature.bmp" };

                var dataForSignature = new List<TLV.TLV>
                {
                    new TLV.TLV
                    {
                        Tag = new byte[] { 0xE0 },
                        InnerTags = new List<TLV.TLV>
                        {
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0xDF, 0xAA, 0x01 },
                                Data = Encoding.ASCII.GetBytes(signatureFile)
                            },
                            // VALUE PAIR
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0xDF, 0xAA, 0x02 },
                                Data = Encoding.ASCII.GetBytes(signatureMessage[0])
                            },
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0xDF, 0xAA, 0x03 },
                                Data = Encoding.ASCII.GetBytes(signatureMessage[1])
                            },
                            // VALUE PAIR
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0xDF, 0xAA, 0x02 },
                                Data = Encoding.ASCII.GetBytes(signatureLogo[0])
                            },
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0xDF, 0xAA, 0x03 },
                                Data = Encoding.ASCII.GetBytes(signatureLogo[1])
                            }
                        }
                    }
                };
                TLV.TLV tlv = new TLV.TLV();
                byte[] dataForSignatureData = tlv.Encode(dataForSignature);

                Debug.WriteLine(ConsoleMessages.DeviceExtendedReset.GetStringValue());
                VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD2, ins = 0xE0, p1 = 0x00, p2 = 0x01, data = dataForSignatureData };
                WriteSingleCmd(command);   // Device Info [D0, 00]

                // First receive is throw away
                deviceResponse = DeviceHTMLResponse.Task.Result;

                ResponseTagsHandler -= GetSignatureResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceResponse;
        }

        private List<TLV.TLV> FormatE0Tag(byte[] hmackey, byte[] generated_hmackey)
        {
            return new List<TLV.TLV>
            {
                new TLV.TLV
                {
                    Tag = new byte[] { 0xE0 },
                    InnerTags = new List<TLV.TLV>
                    {
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x46 },
                            Data = new byte[] { 0x03 }
                        },
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x2E },
                            Data = hmackey
                        },
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xED, 0x15 },
                            Data = generated_hmackey
                        }
                    }
                }
            };
        }

        private string GetCurrentKSNHMAC()
        {
            DeviceSecurityConfiguration = new TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetGeneratedHMACResponseHandler;

            var dataForHMAC = new List<TLV.TLV>
            {
                new TLV.TLV
                {
                    Tag = new byte[] { 0xE0 },
                    InnerTags = new List<TLV.TLV>
                    {
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x0E },
                            Data = new byte[] { 0x00 }
                        },
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x23 },
                            Data = new byte[] { 0x06 }
                        },
                        new TLV.TLV
                        {
                            Tag = new byte[] { 0xDF, 0xEC, 0x23 },
                            Data = new byte[] { 0x07 }
                        }
                    }
                }
            };
            TLV.TLV tlv = new TLV.TLV();
            byte[] dataForHMACData = tlv.Encode(dataForHMAC);

            Debug.WriteLine(ConsoleMessages.UpdateHMACKeys.GetStringValue());
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x22, p1 = 0x00, p2 = 0x00, data = dataForHMACData };
            WriteSingleCmd(command);

            var deviceSecurityConfigurationInfo = DeviceSecurityConfiguration.Task.Result;

            ResponseTagsHandler -= GetGeneratedHMACResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceSecurityConfigurationInfo.securityConfigurationObject.GeneratedHMAC;
        }

        private int UpdateHMACKey(byte keyId, byte[] dataForHMACData)
        {
            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            Debug.WriteLine(ConsoleMessages.UpdateHMACKeys.GetStringValue());
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x0A, p1 = keyId, p2 = 0x01, data = dataForHMACData };
            WriteSingleCmd(command);

            int vipaResponse = ResponseCodeResult.Task.Result;

            ResponseTagsHandler -= ResponseCodeHandler;
            ResponseTagsHandlerSubscribed--;

            return vipaResponse;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) PutFile(string fileName, string targetFile)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return (null, (int)VipaSW1SW2Codes.Failure);
            }

            (BinaryStatusObject binaryStatusObject, int VipaResponse) deviceBinaryStatus = (null, (int)VipaSW1SW2Codes.Failure);

            if (File.Exists(targetFile))
            {
                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetBinaryStatusResponseHandler;

                FileInfo fileInfo = new FileInfo(targetFile);
                long fileLength = fileInfo.Length;
                byte[] fileSize = new byte[4];
                Array.Copy(BitConverter.GetBytes(fileLength), 0, fileSize, 0, fileSize.Length);
                Array.Reverse(fileSize);

                // File information
                var fileInformation = new List<TLV.TLV>
                {
                    new TLV.TLV
                    {
                        Tag = new byte[] { 0x6F },
                        InnerTags = new List<TLV.TLV>
                        {
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0x84 },
                                Data = Encoding.ASCII.GetBytes(fileName.ToLower())
                            },
                            new TLV.TLV
                            {
                                Tag = new byte[] { 0x80 },
                                Data = fileSize
                            }
                        }
                    }
                };
                TLV.TLV tlv = new TLV.TLV();
                byte[] fileInformationData = tlv.Encode(fileInformation);

                DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();
                VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xA5, p1 = 0x05, p2 = 0x81, data = fileInformationData };
                WriteSingleCmd(command);

                // Tag 6F with size and checksum is returned on success
                deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

                //if (vipaResponse == (int)VipaSW1SW2Codes.Success)
                if (deviceBinaryStatus.VipaResponse == (int)VipaSW1SW2Codes.Success)
                {
                    using (FileStream fs = File.OpenRead(targetFile))
                    {
                        int numBytesToRead = (int)fs.Length;

                        while (numBytesToRead > 0)
                        {
                            byte[] readBytes = new byte[PACKET_SIZE];
                            int bytesRead = fs.Read(readBytes, 0, PACKET_SIZE);
                            WriteRawBytes(readBytes);
                            numBytesToRead -= bytesRead;
                        }
                    }

                    // wait for device reponse
                    DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();
                    deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;
                }

                ResponseTagsHandler -= GetBinaryStatusResponseHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return deviceBinaryStatus;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) GetBinaryStatus(string fileName)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetBinaryStatusResponseHandler;

            DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();

            var data = Encoding.ASCII.GetBytes(fileName);
            byte reportMD5 = 0x80;
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xC0, p1 = 0x00, p2 = reportMD5, data = Encoding.ASCII.GetBytes(fileName) };
            WriteSingleCmd(command);

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTagsHandler -= GetBinaryStatusResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) SelectFileForOps(string fileName)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetBinaryStatusResponseHandler;

            // When the file cannot be accessed, VIPA returns SW1SW2 equal to 9F13
            DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();

            var data = Encoding.ASCII.GetBytes(fileName);

            // Bit 2:  1 - Selection by DF name
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xA4, p1 = 0x04, p2 = 0x00, data = Encoding.ASCII.GetBytes(fileName) };
            WriteSingleCmd(command);

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTagsHandler -= GetBinaryStatusResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        private (BinaryStatusObject binaryStatusObject, int VipaResponse) ReadBinaryDataFromSelectedFile(byte readOffset, byte bytesToRead)
        {
            CancelResponseHandlers();

            ResponseTagsHandlerSubscribed++;
            ResponseTaglessHandler += GetBinaryDataResponseHandler;

            // When the file cannot be accessed, VIPA returns SW1SW2 equal to 9F13
            DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();

            // P1 bit 8 = 0: P1 and P2 are the offset at which to read the data from (15-bit addressing)
            // P1 bit 8 = 1: data size 2 bytes, first byte is low-order offset byte, 2nd byte is number of bytes to read
            // DATA - If P1 bit 8 = 0, data size 1 byte, contains the number of bytes to read
            VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xB0, p1 = 0x00, p2 = readOffset };
            command.includeLE = true;
            command.le = bytesToRead;
            WriteSingleCmd(command);

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTaglessHandler -= GetBinaryDataResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        #endregion --- VIPA commands ---

        #region --- response handlers ---

        public void CancelResponseHandlers(int retries = 1)
        {
            int count = 0;

            while (ResponseTagsHandlerSubscribed != 0 && count++ <= retries)
            {
                ResponseTagsHandler?.Invoke(null, (int)VipaSW1SW2Codes.Success, true);
                Thread.Sleep(1);
            }
            //count = 0;
            //while (ResponseTaglessHandlerSubscribed != 0 && count++ <= retries)
            //{
            //    ResponseTaglessHandler?.Invoke(null, -1, true);
            //    Thread.Sleep(1);
            //}
            //count = 0;
            //while (ResponseContactlessHandlerSubscribed != 0 && count++ <= retries)
            //{
            //    ResponseCLessHandler?.Invoke(null, -1, 0, true);
            //    Thread.Sleep(1);
            //}

            ResponseTagsHandlerSubscribed = 0;
            ResponseTagsHandler = null;
            //ResponseTaglessHandlerSubscribed = 0;
            ResponseTaglessHandler = null;
            //ResponseContactlessHandlerSubscribed = 0;
            ResponseCLessHandler = null;
        }

        public void ResponseCodeHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            ResponseCodeResult?.TrySetResult(cancelled ? -1 : responseCode);
        }

        public void DeviceResetResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceResetConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new DevicePTID();

            if (tags.FirstOrDefault().Tag.SequenceEqual(E0Template.PtidTag))
            {
                deviceResponse.PTID = BitConverter.ToString(tags.FirstOrDefault().Data).Replace("-", "");
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count == 1)
                {
                    DeviceResetConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceResetConfiguration?.TrySetResult((null, responseCode));
            }
        }

        private void GetDeviceInfoResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                DeviceIdentifier?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new LinkDeviceResponse
            {
                // TODO: rework to be values reflecting actual device capabilities
                /*CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                {
                    CardCaptureTimeout = 90,
                    ManualCardTimeout = 5,
                    DebitEnabled = false,
                    EMVEnabled = false,
                    ContactlessEnabled = false,
                    ContactlessEMVEnabled = false,
                    CVVEnabled = false,
                    VerifyAmountEnabled = false,
                    AVSEnabled = false,
                    SignatureEnabled = false
                }*/
            };

            LinkDALRequestIPA5Object cardInfo = new LinkDALRequestIPA5Object();

            foreach (var tag in tags)
            {
                if (tag.Tag.SequenceEqual(EETemplate.EETemplateTag))
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag.SequenceEqual(EETemplate.TerminalNameTag) && string.IsNullOrEmpty(deviceResponse.Model))
                        {
                            deviceResponse.Model = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag.SequenceEqual(EETemplate.SerialNumberTag) && string.IsNullOrWhiteSpace(deviceResponse.SerialNumber))
                        {
                            deviceResponse.SerialNumber = Encoding.UTF8.GetString(dataTag.Data);
                            //deviceInformation.SerialNumber = deviceResponse.SerialNumber ?? string.Empty;
                        }
                        else if (dataTag.Tag.SequenceEqual(EETemplate.TamperStatus))
                        {
                            //DF8101 = 00 no tamper detected
                            //DF8101 = 01 tamper detected
                            //cardInfo.TamperStatus = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag.SequenceEqual(EETemplate.ArsStatus))
                        {
                            //DF8102 = 00 ARS not active
                            //DF8102 = 01 ARS active
                            //cardInfo.ArsStatus = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
                else if (tag.Tag.SequenceEqual(EETemplate.TerminalIdTag))
                {
                    //deviceResponse.TerminalId = Encoding.UTF8.GetString(tag.Data);
                }
                else if (tag.Tag.SequenceEqual(EFTemplate.EFTemplateTag))
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag.SequenceEqual(EFTemplate.WhiteListHash))
                        {
                            //cardInfo.WhiteListHash = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag.SequenceEqual(EFTemplate.FirmwareVersion) && string.IsNullOrWhiteSpace(deviceResponse.FirmwareVersion))
                        {
                            deviceResponse.FirmwareVersion = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
                else if (tag.Tag.SequenceEqual(E6Template.E6TemplateTag))
                {
                    deviceResponse.PowerOnNotification = new XO.Responses.Device.LinkDevicePowerOnNotification();

                    TLV.TLV tlv = new TLV.TLV();
                    var _tags = tlv.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag.SequenceEqual(E6Template.TransactionStatusTag))
                        {
                            deviceResponse.PowerOnNotification.TransactionStatus = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag.SequenceEqual(E6Template.TransactionStatusMessageTag))
                        {
                            deviceResponse.PowerOnNotification.TransactionStatusMessage = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag.SequenceEqual(EETemplate.TerminalIdTag))
                        {
                            deviceResponse.PowerOnNotification.TerminalID = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags?.Count > 0)
                {
                    DeviceInfoObject deviceInfoObject = new DeviceInfoObject
                    {
                        LinkDeviceResponse = deviceResponse,
                        LinkDALRequestIPA5Object = cardInfo
                    };
                    DeviceIdentifier?.TrySetResult((deviceInfoObject, responseCode));
                }
                //else
                //{
                //    deviceIdentifier?.TrySetResult((null, responseCode));
                //}
            }
        }

        public void GetSecurityInformationResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new SecurityConfigurationObject();

            foreach (var tag in tags)
            {
                if (tag.Tag.SequenceEqual(E0Template.E0TemplateTag))
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag.SequenceEqual(E0Template.OnlinePINKSNTag))
                        {
                            deviceResponse.OnlinePinKSN = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        if (dataTag.Tag.SequenceEqual(E0Template.KeySlotNumberTag))
                        {
                            deviceResponse.KeySlotNumber = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag.SequenceEqual(E0Template.SRedCardKSNTag))
                        {
                            deviceResponse.SRedCardKSN = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag.SequenceEqual(E0Template.InitVectorTag))
                        {
                            deviceResponse.InitVector = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag.SequenceEqual(E0Template.EncryptedKeyCheckTag))
                        {
                            deviceResponse.EncryptedKeyCheck = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    DeviceSecurityConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
            }
        }

        public void GetKernelInformationResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceKernelConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new KernelConfigurationObject();

            foreach (var tag in tags)
            {
                // note: we just need the first instance
                if (tag.Tag.SequenceEqual(E0Template.E0TemplateTag))
                {
                    var kernelApplicationTag = tag.InnerTags.Where(x => x.Tag.SequenceEqual(E0Template.ApplicationAIDTag)).FirstOrDefault();
                    deviceResponse.ApplicationIdentifierTerminal = BitConverter.ToString(kernelApplicationTag.Data).Replace("-", "");
                    var kernelChecksumTag = tag.InnerTags.Where(x => x.Tag.SequenceEqual(E0Template.KernelConfigurationTag)).FirstOrDefault();
                    deviceResponse.ApplicationKernelInformation = ConversionHelper.ByteArrayToAsciiString(kernelChecksumTag.Data).Replace("\0", string.Empty);
                    break;
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    DeviceKernelConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceKernelConfiguration?.TrySetResult((null, responseCode));
            }
        }

        public void GetGeneratedHMACResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            var MACTag = new byte[] { 0xDF, 0xEC, 0x7B };

            if (cancelled || tags == null)
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new SecurityConfigurationObject();

            if (tags.FirstOrDefault().Tag.SequenceEqual(MACTag))
            {
                deviceResponse.GeneratedHMAC = BitConverter.ToString(tags.FirstOrDefault().Data).Replace("-", "");
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count == 1)
                {
                    DeviceSecurityConfiguration?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
            }
        }

        public void GetBinaryStatusResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceBinaryStatusInformation?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new BinaryStatusObject();

            foreach (var tag in tags)
            {
                if (tag.Tag.SequenceEqual(_6FTemplate._6fTemplateTag))
                {
                    TLV.TLV tlv = new TLV.TLV();
                    var _tags = tlv.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag.SequenceEqual(_6FTemplate.FileSizeTag))
                        {
                            deviceResponse.FileSize = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag.SequenceEqual(_6FTemplate.FileCheckSumTag))
                        {
                            deviceResponse.FileCheckSum = BitConverter.ToString(dataTag.Data, 0).Replace("-", "");
                        }
                        else if (dataTag.Tag.SequenceEqual(_6FTemplate.SecurityStatusTag))
                        {
                            deviceResponse.SecurityStatus = BCDConversion.BCDToInt(dataTag.Data);
                        }
                    }

                    break;
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                // command could return just a response without tags
                DeviceBinaryStatusInformation?.TrySetResult((deviceResponse, responseCode));
            }
            else
            {
                deviceResponse.FileNotFound = true;
                DeviceBinaryStatusInformation?.TrySetResult((deviceResponse, responseCode));
            }
        }

        public void GetBinaryDataResponseHandler(byte[] data, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                DeviceBinaryStatusInformation?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new BinaryStatusObject();

            if (responseCode == (int)VipaSW1SW2Codes.Success && data?.Length > 0)
            {
                deviceResponse.ReadResponseBytes = ArrayPool<byte>.Shared.Rent(data.Length);
                Array.Copy(data, 0, deviceResponse.ReadResponseBytes, 0, data.Length);
            }
            else
            {
                deviceResponse.FileNotFound = true;
            }

            DeviceBinaryStatusInformation?.TrySetResult((deviceResponse, responseCode));
        }

        public void GetSignatureResponseHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false)
        {
            var htmlKeyTag = new byte[] { 0xDF, 0xAA, 0x02 };
            var htmlValTag = new byte[] { 0xDF, 0xAA, 0x03 };
            var htmlResultsTag = new byte[] { 0xDF, 0xAA, 0x05 };

            if (cancelled || tags == null)
            {
                DeviceHTMLResponse?.TrySetResult((null, responseCode));
                return;
            }

            // Wait for next collection
            if (tags.Count == 0)
            {
                return;
            }

            var deviceResponse = new HTMLResponseObject();
            deviceResponse.HTMLValueBytes = new List<byte[]>();

            if (tags.FirstOrDefault().Tag.SequenceEqual(htmlResultsTag))
            {
                foreach (var tag in tags)
                {
                    if (tag.Tag.SequenceEqual(htmlValTag) && tag.Data.Length > 0)
                    {
                        byte[] worker = ArrayPool<byte>.Shared.Rent(tag.Data.Length);
                        Array.Copy(tag.Data, 0, worker, 0, tag.Data.Length);
                        deviceResponse.HTMLValueBytes.Add(worker);
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 1)
                {
                    DeviceHTMLResponse?.TrySetResult((deviceResponse, responseCode));
                }
            }
            else
            {
                DeviceHTMLResponse?.TrySetResult((null, responseCode));
            }
        }

        #endregion --- response handlers ---
    }
}
