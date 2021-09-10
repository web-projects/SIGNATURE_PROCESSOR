using Config.Config;
using Devices.Common.Helpers;
using Devices.Common.Helpers.Templates;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA.Interfaces;
using Devices.Verifone.VIPA.TagLengthValue;
using SignatureProcessorApp.common.xo.Responses.DAL;
using SignatureProcessorApp.devices.Verifone.Helpers;
using SignatureProcessorApp.devices.Verifone.VIPA.Helpers;
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
    public class VIPAImpl : IVIPA, IDisposable
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

        public delegate void ResponseTagsHandlerDelegate(List<TLV> tags, int responseCode, bool cancelled = false);
        internal ResponseTagsHandlerDelegate ResponseTagsHandler = null;

        public delegate void ResponseTaglessHandlerDelegate(byte[] data, int dataLength, int responseCode, bool cancelled = false);
        internal ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;

        public delegate void ResponseCLessHandlerDelegate(List<TLV> tags, int responseCode, int pcb, bool cancelled = false);
        internal ResponseCLessHandlerDelegate ResponseCLessHandler = null;

        public TaskCompletionSource<(DevicePTID devicePTID, int VipaResponse)> DeviceResetConfiguration = null;

        public TaskCompletionSource<(DeviceInfoObject deviceInfoObject, int VipaResponse)> DeviceIdentifier = null;
        public TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)> DeviceSecurityConfiguration = null;
        public TaskCompletionSource<(KernelConfigurationObject kernelConfigurationObject, int VipaResponse)> DeviceKernelConfiguration = null;

        public TaskCompletionSource<(string HMAC, int VipaResponse)> DeviceGenerateHMAC = null;
        public TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)> DeviceBinaryStatusInformation = null;

        public TaskCompletionSource<(HTMLResponseObject htmlResponseObject, int VipaResponse)> DeviceHTMLResponse = null;

        public TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)> DeviceInteractionInformation { get; set; } = null;

        private List<byte[]> signaturePayload = null;

        #endregion --- attributes ---

        #region --- connection ---
        private SerialConnection VerifoneConnection { get; set; }

        public bool Connect(string comPort, SerialConnection connection)
        {
            VerifoneConnection = connection;
            return VerifoneConnection.Connect();
        }

        public bool IsConnected()
        {
            return VerifoneConnection?.IsConnected() ?? false;
        }

        public void Dispose()
        {
            VerifoneConnection?.Dispose();
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

        private bool ContactlessReaderInitialized;
        #endregion --- resources ---

        private void WriteSingleCmd(VIPACommand command)
        {
            VerifoneConnection?.WriteSingleCmd(new VIPAResponseHandlers
            {
                responsetagshandler = ResponseTagsHandler,
                responsetaglesshandler = ResponseTaglessHandler,
                responsecontactlesshandler = ResponseCLessHandler
            }, command);
        }

        private void WriteRawBytes(byte[] buffer)
        {
            VerifoneConnection?.WriteRaw(buffer, buffer.Length);
        }

        private void SendVipaCommand(VIPACommandType commandType, byte p1, byte p2, byte[] data = null, byte nad = 0x1, byte pcb = 0x0)
        {
            Debug.WriteLine($"Send VIPA {commandType}");
            VIPACommand command = new VIPACommand(commandType) { nad = nad, pcb = pcb, p1 = p1, p2 = p2, data = data };
            WriteSingleCmd(command);
        }

        #region --- VIPA commands ---

        /// <summary>
        /// Force Closing contactless reader regardless of open state to avoid displaying of the UI status bar.
        /// When the contactless reader is opened and device is disconnected, there's not a way for DAL to know if the reader was opened before.
        /// By force-closing the reader, the idle screen will not display the contactless UI status bar.
        /// </summary>
        /// <returns></returns>
        public int CloseContactlessReader(bool forceClose = false)
        {
            int commandResult = (int)VipaSW1SW2Codes.Failure;

            // Close only the reader when a forms update is performed
            if (ContactlessReaderInitialized || forceClose)
            {
                ContactlessReaderInitialized = false;

                ResponseCodeResult = new TaskCompletionSource<int>();

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += ResponseCodeHandler;

                SendVipaCommand(VIPACommandType.CloseContactlessReader, 0x00, 0x00);   // Close CLess Reader [C0, 02]

                commandResult = ResponseCodeResult.Task.Result;

                ResponseTagsHandler -= ResponseCodeHandler;
                ResponseTagsHandlerSubscribed--;
            }

            return commandResult;
        }

        public bool DisplayMessage(VIPADisplayMessageValue displayMessageValue = VIPADisplayMessageValue.Idle, bool enableBacklight = false, string customMessage = "")
        {
            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD2, ins = 0x01, p1 = (byte)displayMessageValue, p2 = (byte)(enableBacklight ? 0x01 : 0x00), data = Encoding.ASCII.GetBytes(customMessage) };
            //WriteSingleCmd(command);   
            // Display [D2, 01]
            SendVipaCommand(VIPACommandType.Display, (byte)displayMessageValue, (byte)(enableBacklight ? 0x01 : 0x00), Encoding.ASCII.GetBytes(customMessage));

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0xFF, p1 = 0x00, p2 = 0x00 };
            //WriteSingleCmd(command);
            SendVipaCommand(VIPACommandType.Abort, 0x00, 0x00);

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
                //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x00, p1 = 0x00, p2 = (byte)(ResetDeviceCfg.ReturnSerialNumber | ResetDeviceCfg.ReturnAfterCardRemoval | ResetDeviceCfg.ReturnPinpadConfiguration) };
                //WriteSingleCmd(command);   
                // Reset Device [D0, 00]
                SendVipaCommand(VIPACommandType.ResetDevice, 0x00, (byte)(ResetDeviceCfg.ReturnSerialNumber | ResetDeviceCfg.ReturnAfterCardRemoval | ResetDeviceCfg.ReturnPinpadConfiguration));

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
                TLV dataForReset = new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(E0Template.ResetDeviceFlags, new byte[] { 0x02, 0x0F })
                    }
                };

                byte[] dataForResetData = TLV.Encode(dataForReset);

                Debug.WriteLine(ConsoleMessages.DeviceExtendedReset.GetStringValue());
                //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x0A, p1 = 0x00, p2 = 0x00, data = dataForResetData };
                //WriteSingleCmd(command);   
                // Reset Device [D0, 00]
                SendVipaCommand(VIPACommandType.ResetDevice, 0x00, 0x00, dataForResetData);

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x00, p1 = 0x01, p2 = 0x03 };
            //WriteSingleCmd(command);
            // Reset Device [D0, 00]
            SendVipaCommand(VIPACommandType.ResetDevice, 0x01, 0x03);

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x00, p1 = 0x01, p2 = 0x00 };
            //WriteSingleCmd(command);
            // Reset Device [D0, 00]
            SendVipaCommand(VIPACommandType.ResetDevice, 0x01, 0x00);

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

            List<TLV> aidRequestedTransaction = new List<TLV>
            {
                new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(E0Template.EMVKernelAidGenerator, new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x10, 0x10 })  // AID A000000003101001
                    }
                }
            };
            var aidRequestedTransactionData = TLV.Encode(aidRequestedTransaction);

            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xDE, ins = 0x01, p1 = 0x00, p2 = 0x00, data = aidRequestedTransactionData };
            //WriteSingleCmd(command);
            // Get EMV Hash Values [DE, 01]
            SendVipaCommand(VIPACommandType.GetEMVHashValues, 0x00, 0x00, aidRequestedTransactionData);

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

            Debug.WriteLine(ConsoleMessages.GetSecurityConfiguration.GetStringValue());
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x11, p1 = vssSlot, p2 = hostID };
            //WriteSingleCmd(command);
            // Get Security Configuation [C4, 11]
            SendVipaCommand(VIPACommandType.GetSecurityConfiguration, vssSlot, hostID);

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

            var dataForHMAC = new TLV
            {
                Tag = E0Template.E0TemplateTag,
                InnerTags = new List<TLV>
                {
                    new TLV(E0Template.MACGenerationData, ConversionHelper.HexToByteArray(MAC)),
                    new TLV(E0Template.MACHostId, new byte[] { Convert.ToByte(hostID) })
                }
            };
            var dataForHMACData = TLV.Encode(dataForHMAC);

            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x22, p1 = 0x00, p2 = 0x00, data = dataForHMACData };
            //WriteSingleCmd(command);
            // Generate HMAC [C4, 22]
            SendVipaCommand(VIPACommandType.GenerateHMAC, 0x00, 0x00, dataForHMACData);

            var deviceSecurityConfigurationInfo = DeviceSecurityConfiguration.Task.Result;

            ResponseTagsHandler -= GetGeneratedHMACResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceSecurityConfigurationInfo;
        }

        public int UpdateHMACKeys()
        {
            //string generatedHMAC = GetCurrentKSNHMAC();

            // KEY 06 Generation
            //byte[] hmac_generated_key = ConversionHelper.HexToByteArray(generatedHMAC);

            // Signature = HMAC_old(old XOR new) - array1 is smaller or equal in size as array2
            //byte[] hmac_signature_06 = ConversionHelper.XORArrays(hmac_generated_key, HMACValidator.HMACKEY06);

            //var dataKey06HMAC = FormatE0Tag(HMACValidator.HMACKEY06, hmac_signature_06);
            //byte[] dataForHMACData = TLV.Encode(dataKey06HMAC);

            // key slot 06
            //int vipaResponse = UpdateHMACKey(0x06, dataForHMACData);

            //if (vipaResponse == (int)VipaSW1SW2Codes.Success)
            //{
            //    // KEY 07 Generation
            //    byte[] hmac_signature_07 = ConversionHelper.XORArrays(hmac_generated_key, HMACValidator.HMACKEY07);

            //    var dataKey07HMAC = FormatE0Tag(HMACValidator.HMACKEY07, hmac_signature_07);
            //    dataForHMACData = TLV.Encode(dataKey07HMAC);

            //    // key slot 07
            //    vipaResponse = UpdateHMACKey(0x07, dataForHMACData);
            //}

            //return vipaResponse;

            return 0;
        }

        public (HTMLResponseObject htmlResponseObject, int VipaResponse) GetSignature()
        {
            (HTMLResponseObject HTMLResponseObject, int VipaResponse) deviceResponse = (null, (int)VipaSW1SW2Codes.Failure);

            // abort previous user entries in progress
            (int VipaData, int VipaResponse) vipaResult = DeviceCommandAbort();

            if (vipaResult.VipaResponse == (int)VipaSW1SW2Codes.Success)
            {
                // Setup keyboard reader
                //if ((int)VipaSW1SW2Codes.Success != StartKeyboardReader())
                //{
                //    return (null, (int)VipaSW1SW2Codes.Failure);
                //}

                DeviceHTMLResponse = new TaskCompletionSource<(HTMLResponseObject HTMLResponseObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);

                ResponseTagsHandlerSubscribed++;
                ResponseTagsHandler += GetSignatureResponseHandler;

                byte[] signatureFile = Encoding.ASCII.GetBytes("mapp/signature.html");
                byte[][] signatureMessage = { Encoding.ASCII.GetBytes("please_sign_text"), Encoding.ASCII.GetBytes("ENTER SIGNATURE") };
                byte[][] signatureLogo = { Encoding.ASCII.GetBytes("logo_image"), Encoding.ASCII.GetBytes("signature.bmp") };

                var getSignatureData = new TLV
                {
                    Tag = E0Template.E0TemplateTag,
                    InnerTags = new List<TLV>
                    {
                        new TLV(SignatureTemplate.SignatureFile, signatureFile),
                        new TLV(SignatureTemplate.HTMLKey, signatureMessage[0]),
                        new TLV(SignatureTemplate.HTMLValue, signatureMessage[1]),
                        new TLV(SignatureTemplate.HTMLKey, signatureLogo[0]),
                        new TLV(SignatureTemplate.HTMLValue, signatureLogo[1])
                    }
                };

                byte[] dataForSignatureData = TLV.Encode(getSignatureData);

                ResponseCodeResult = new TaskCompletionSource<int>();

                Debug.WriteLine(ConsoleMessages.GetSignature.GetStringValue());
                //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD2, ins = 0xE0, p1 = 0x00, p2 = 0x01, data = dataForSignatureData };
                //WriteSingleCmd(command);   
                // Display HTML [D2, E0]
                SendVipaCommand(VIPACommandType.DisplayHTML, 0x00, 0x01, dataForSignatureData);

                //(int vipaResponse, int vipaData) commandResult = (ResponseCodeResult.Task.Result, 0);
                // First receive is throw away
                deviceResponse = DeviceHTMLResponse.Task.Result;

                //if (commandResult.vipaResponse == (int)VipaSW1SW2Codes.Success)
                //if (deviceResponse.VipaResponse == (int)VipaSW1SW2Codes.Success)
                //{
                //    DeviceHTMLResponse = new TaskCompletionSource<(HTMLResponseObject HTMLResponseObject, int VipaResponse)>(TaskCreationOptions.RunContinuationsAsynchronously);
                //    deviceResponse = DeviceHTMLResponse.Task.Result;

                //    do
                //    {
                //        LinkDALRequestIPA5Object cardInfo = DeviceInteractionInformation.Task.Result.linkDALRequestIPA5Object;
                //        commandResult.vipaResponse = DeviceInteractionInformation.Task.Result.VipaResponse;

                //        // First receive is throw away
                //        //deviceResponse = DeviceHTMLResponse.Task.Result;

                //        if (cardInfo?.DALResponseData?.Status?.Equals("UserKeyPressed") ?? false)
                //        {
                //            Debug.WriteLine($"KEY PRESSED: {cardInfo.DALResponseData.Value}");
                //            Console.WriteLine($"KEY PRESSED: {cardInfo.DALResponseData.Value}");
                //            // <O> == 1 : YES
                //            // <X> == 2 : NO
                //            if (cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_1.ToString()) || cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_OK.ToString()))
                //            {
                //                commandResult.vipaData = 1;
                //            }
                //            else if (cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_2.ToString()) || cardInfo.DALResponseData.Value.Equals(DeviceKeys.KEY_STOP.ToString()))
                //            {
                //                commandResult.vipaData = 0;
                //            }
                //            else
                //            {
                //                commandResult.vipaResponse = (int)VipaSW1SW2Codes.Failure;
                //                DeviceInteractionInformation = new TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)>();
                //            }
                //        }

                //    } while (commandResult.vipaResponse == (int)VipaSW1SW2Codes.Failure);

                //    ResponseTagsHandler -= GetSignatureResponseHandler;
                //    ResponseTagsHandlerSubscribed--;
                //}

                //// Stop keyboard reader
                //StopKeyboardReader();
                //}
            }

            return deviceResponse;
        }

        //private List<TLV> FormatE0Tag(byte[] hmackey, byte[] generated_hmackey)
        //{
        //    return new List<TLV>
        //    {
        //        new TLV
        //        {
        //            Tag = new byte[] { 0xE0 },
        //            InnerTags = new List<TLV>
        //            {
        //                new TLV
        //                {
        //                    Tag = new byte[] { 0xDF, 0xEC, 0x46 },
        //                    Data = new byte[] { 0x03 }
        //                },
        //                new TLV
        //                {
        //                    Tag = new byte[] { 0xDF, 0xEC, 0x2E },
        //                    Data = hmackey
        //                },
        //                new TLV
        //                {
        //                    Tag = new byte[] { 0xDF, 0xED, 0x15 },
        //                    Data = generated_hmackey
        //                }
        //            }
        //        }
        //    };
        //}

        private string GetCurrentKSNHMAC(int hostID, string MAC)
        {
            DeviceSecurityConfiguration = new TaskCompletionSource<(SecurityConfigurationObject securityConfigurationObject, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetGeneratedHMACResponseHandler;

            var messageForHMAC = new TLV
            {
                Tag = E0Template.E0TemplateTag,
                InnerTags = new List<TLV>
                {
                    new TLV(E0Template.MACGenerationData, ConversionHelper.HexToByteArray(MAC) ),
                    new TLV(E0Template.MACHostId, new byte[] { Convert.ToByte(hostID) })
                }
            };
            byte[] dataForHMACData = TLV.Encode(messageForHMAC);

            Debug.WriteLine(ConsoleMessages.UpdateHMACKeys.GetStringValue());
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x22, p1 = 0x00, p2 = 0x00, data = dataForHMACData };
            //WriteSingleCmd(command);
            // Generate HMAC [C4, 22]
            SendVipaCommand(VIPACommandType.GenerateHMAC, 0x00, 0x00, dataForHMACData);

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xC4, ins = 0x0A, p1 = keyId, p2 = 0x01, data = dataForHMACData };
            //WriteSingleCmd(command);
            // Update Key [C4, 0A]
            SendVipaCommand(VIPACommandType.UpdateKey, 0x00, 0x00, dataForHMACData);

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
                byte[] streamSize = new byte[4];
                Array.Copy(BitConverter.GetBytes(fileLength), 0, streamSize, 0, streamSize.Length);
                Array.Reverse(streamSize);

                // File information
                var fileInformation = new TLV
                {
                    Tag = _6FTemplate._6fTemplateTag,
                    InnerTags = new List<TLV>()
                    {
                        new TLV(_6FTemplate.FileNameTag, Encoding.UTF8.GetBytes(fileName)),
                        new TLV(_6FTemplate.FileSizeTag, streamSize),
                    }
                };
                byte[] fileInformationData = TLV.Encode(fileInformation);

                DeviceBinaryStatusInformation = new TaskCompletionSource<(BinaryStatusObject binaryStatusObject, int VipaResponse)>();
                //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xA5, p1 = 0x05, p2 = 0x81, data = fileInformationData };
                //WriteSingleCmd(command);
                // Stream Upload [00, A5]
                SendVipaCommand(VIPACommandType.StreamUpload, 0x05, 0x81, fileInformationData);

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xC0, p1 = 0x00, p2 = reportMD5, data = Encoding.ASCII.GetBytes(fileName) };
            //WriteSingleCmd(command);
            // Get Binary Status [00, C0]
            SendVipaCommand(VIPACommandType.GetBinaryStatus, 0x00, reportMD5, Encoding.ASCII.GetBytes(fileName));

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xA4, p1 = 0x04, p2 = 0x00, data = Encoding.ASCII.GetBytes(fileName) };
            //WriteSingleCmd(command);
            // Select File [00, A4]
            SendVipaCommand(VIPACommandType.SelectFile, 0x04, 0x00, Encoding.ASCII.GetBytes(fileName));

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
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0x00, ins = 0xB0, p1 = 0x00, p2 = readOffset };
            //command.includeLE = true;
            //command.le = bytesToRead;
            VIPACommand command = new VIPACommand(VIPACommandType.ReadBinary) { nad = 0x1, pcb = 0x00, p1 = 0x00, p2 = readOffset, includeLE = true, le = bytesToRead };
            // Read Binary [00, B0]
            WriteSingleCmd(command);

            var deviceBinaryStatus = DeviceBinaryStatusInformation.Task.Result;

            ResponseTaglessHandler -= GetBinaryDataResponseHandler;
            ResponseTagsHandlerSubscribed--;

            return deviceBinaryStatus;
        }

        private int StartKeyboardReader()
        {
            CancelResponseHandlers();

            ResponseCodeResult = new TaskCompletionSource<int>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += ResponseCodeHandler;

            // Setup reader to accept user input
            DeviceInteractionInformation = new TaskCompletionSource<(LinkDALRequestIPA5Object linkDALRequestIPA5Object, int VipaResponse)>();

            ResponseTagsHandlerSubscribed++;
            ResponseTagsHandler += GetDeviceInteractionKeyboardResponseHandler;

            // collect response from user
            // Bit 0 - Enter, Cancel, Clear keys
            // Bit 1 - function keys
            // Bit 2 - numeric keys
            //SendVipaCommand(VIPACommandType.KeyboardStatus, 0x07, 0x00);
            Debug.WriteLine(ConsoleMessages.KeyboardStatus.GetStringValue());
            //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x61, p1 = 0x07, p2 = 0x00 };
            //WriteSingleCmd(command);   
            // Keyboard Status [D0, 00]
            SendVipaCommand(VIPACommandType.KeyboardStatus, 0x07, 0x00);

            return ResponseCodeResult.Task.Result;
        }

        private int StopKeyboardReader()
        {
            if (ResponseTagsHandlerSubscribed > 0)
            {
                //SendVipaCommand(VIPACommandType.KeyboardStatus, 0x00, 0x00);
                Debug.WriteLine(ConsoleMessages.KeyboardStatus.GetStringValue());
                //VIPACommand command = new VIPACommand { nad = 0x01, pcb = 0x00, cla = 0xD0, ins = 0x61, p1 = 0x00, p2 = 0x00 };
                //WriteSingleCmd(command);   
                // Keyboard Status [D0, 61]
                SendVipaCommand(VIPACommandType.KeyboardStatus, 0x00, 0x00);

                int response = DeviceInteractionInformation.Task.Result.VipaResponse;

                ResponseTagsHandler -= GetDeviceInteractionKeyboardResponseHandler;
                ResponseTagsHandlerSubscribed--;

                return response;
            }

            return (int)VipaSW1SW2Codes.Failure;
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

        public void ResponseCodeHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            ResponseCodeResult?.TrySetResult(cancelled ? -1 : responseCode);
        }

        public void DeviceResetResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceResetConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new DevicePTID();

            if (tags.FirstOrDefault().Tag == EETemplate.TerminalId)
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

        private void GetDeviceInfoResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
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
                if (tag.Tag == EETemplate.EETemplateTag)
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == EETemplate.TerminalName && string.IsNullOrEmpty(deviceResponse.Model))
                        {
                            deviceResponse.Model = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag == EETemplate.SerialNumber && string.IsNullOrWhiteSpace(deviceResponse.SerialNumber))
                        {
                            deviceResponse.SerialNumber = Encoding.UTF8.GetString(dataTag.Data);
                            //deviceInformation.SerialNumber = deviceResponse.SerialNumber ?? string.Empty;
                        }
                        else if (dataTag.Tag == EETemplate.TamperStatus)
                        {
                            //DF8101 = 00 no tamper detected
                            //DF8101 = 01 tamper detected
                            //cardInfo.TamperStatus = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag == EETemplate.ArsStatus)
                        {
                            //DF8102 = 00 ARS not active
                            //DF8102 = 01 ARS active
                            //cardInfo.ArsStatus = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
                else if (tag.Tag == EETemplate.TerminalId)
                {
                    //deviceResponse.TerminalId = Encoding.UTF8.GetString(tag.Data);
                }
                else if (tag.Tag == EFTemplate.EFTemplateTag)
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == EFTemplate.WhiteListHash)
                        {
                            //cardInfo.WhiteListHash = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == EFTemplate.FirmwareVersion && string.IsNullOrWhiteSpace(deviceResponse.FirmwareVersion))
                        {
                            deviceResponse.FirmwareVersion = Encoding.UTF8.GetString(dataTag.Data);
                        }
                    }
                }
                else if (tag.Tag == E6Template.E6TemplateTag)
                {
                    deviceResponse.PowerOnNotification = new XO.Responses.Device.LinkDevicePowerOnNotification();

                    var _tags = TLV.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag == E6Template.TransactionStatus)
                        {
                            deviceResponse.PowerOnNotification.TransactionStatus = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag == E6Template.TransactionStatusMessage)
                        {
                            deviceResponse.PowerOnNotification.TransactionStatusMessage = Encoding.UTF8.GetString(dataTag.Data);
                        }
                        else if (dataTag.Tag == EETemplate.TerminalId)
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

        public void GetSecurityInformationResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)

        {
            if (cancelled || tags == null)
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new SecurityConfigurationObject();

            foreach (var tag in tags)
            {
                if (tag.Tag == E0Template.E0TemplateTag)
                {
                    foreach (var dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == E0Template.OnlinePINKSN)
                        {
                            deviceResponse.OnlinePinKSN = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        if (dataTag.Tag == E0Template.KeySlotNumber)
                        {
                            deviceResponse.KeySlotNumber = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == E0Template.SRedCardKSN)
                        {
                            deviceResponse.SRedCardKSN = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == E0Template.InitVector)
                        {
                            deviceResponse.InitVector = BitConverter.ToString(dataTag.Data).Replace("-", "");
                        }
                        else if (dataTag.Tag == E0Template.EncryptedKeyCheck)
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

        public void GetKernelInformationResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
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
                if (tag.Tag == E0Template.E0TemplateTag)
                {
                    var kernelApplicationTag = tag.InnerTags.Where(x => x.Tag == E0Template.ApplicationAID).FirstOrDefault();
                    deviceResponse.ApplicationIdentifierTerminal = BitConverter.ToString(kernelApplicationTag.Data).Replace("-", "");
                    var kernelChecksumTag = tag.InnerTags.Where(x => x.Tag == E0Template.KernelConfiguration).FirstOrDefault();
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

        public void GetGeneratedHMACResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceSecurityConfiguration?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new SecurityConfigurationObject();

            if (tags[0].Tag == E0Template.Cryptogram)
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

        public void GetBinaryStatusResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled || tags == null)
            {
                DeviceBinaryStatusInformation?.TrySetResult((null, responseCode));
                return;
            }

            var deviceResponse = new BinaryStatusObject();

            foreach (var tag in tags)
            {
                if (tag.Tag == _6FTemplate._6fTemplateTag)
                {
                    var _tags = TLV.Decode(tag.Data, 0, tag.Data.Length);

                    foreach (var dataTag in _tags)
                    {
                        if (dataTag.Tag == _6FTemplate.FileSizeTag)
                        {
                            deviceResponse.FileSize = BCDConversion.BCDToInt(dataTag.Data);
                        }
                        else if (dataTag.Tag == _6FTemplate.FileCheckSumTag)
                        {
                            deviceResponse.FileCheckSum = BitConverter.ToString(dataTag.Data, 0).Replace("-", "");
                        }
                        else if (dataTag.Tag == _6FTemplate.SecurityStatusTag)
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

        public void GetBinaryDataResponseHandler(byte[] data, int dataLength, int responseCode, bool cancelled = false)
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

        public void GetSignatureResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            if (cancelled)
            {
                int response = responseCode == (int)VipaSW1SW2Codes.Success ? (int)VipaSW1SW2Codes.UserEntryCancelled : responseCode;
                DeviceInteractionInformation?.TrySetResult((null, response));
                return;
            }

            bool okButtonPressed = false;
            bool collectPoints = false;
            LinkDALRequestIPA5Object deviceResponse = new LinkDALRequestIPA5Object();
            deviceResponse.SignatureData = new List<byte[]>();

            if (responseCode == (int)VipaSW1SW2Codes.Success && tags != null && tags.Count > 0)
            {
                foreach (TLV tag in tags)
                {
                    if (tag.Tag == SignatureTemplate.HTMLKey)
                    {
                        string signatureName = Encoding.UTF8.GetString(tag.Data).Replace("-", "");
                        if (signatureName.Equals("signatureTwo", StringComparison.OrdinalIgnoreCase))
                        {
                            deviceResponse.SignatureName = Encoding.UTF8.GetString(tag.Data).Replace("-", "");
                            collectPoints = true;
                        }
                    }
                    else if (tag.Tag == SignatureTemplate.HTMLValue && tag.Data.Length > 0 && collectPoints)
                    {
                        collectPoints = false;
                        byte[] worker = ArrayPool<byte>.Shared.Rent(tag.Data.Length);
                        Array.Copy(tag.Data, 0, worker, 0, tag.Data.Length);
                        deviceResponse.SignatureData.Add(worker);
                    }
                    else if (tag.Tag == SignatureTemplate.HTMLResponse)
                    {
                        int responseStatus = BCDConversion.BCDToInt(tag.Data);
                        if (responseStatus == 0)
                        {
                            okButtonPressed = true;
                        }
                        else if (responseStatus == (int)DeviceKeys.KEY_CORR)
                        {
                            DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.UserEntryCorrected));
                            return;
                        }
                        else
                        {
                            DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.UserEntryCancelled));
                            return;
                        }
                    }
                }
            }

            if (responseCode == (int)VipaSW1SW2Codes.Success)
            {
                if (tags.Count > 0)
                {
                    if (deviceResponse.SignatureData is { } && deviceResponse.SignatureData.Count > 0)
                    {
                        if (Buffer.ByteLength(deviceResponse.SignatureData[0]) > 0)
                        {
                            signaturePayload = deviceResponse.SignatureData;
                            DeviceInteractionInformation?.TrySetResult((deviceResponse, responseCode));
                        }
                    }
                    else if (okButtonPressed)
                    {
                        DeviceInteractionInformation?.TrySetResult((null, (int)VipaSW1SW2Codes.DataMissing));
                    }
                }
            }
            else
            {
                // log error responses for device troubleshooting purposes
                //DeviceLogger(LogLevel.Error, string.Format("VIPA STATUS CODE=0x{0:X4}", responseCode));
                Debug.WriteLine(string.Format("VIPA STATUS CODE=0x{0:X4}", responseCode));
                DeviceInteractionInformation?.TrySetResult((null, responseCode));
            }
        }

        public void GetDeviceInteractionKeyboardResponseHandler(List<TLV> tags, int responseCode, bool cancelled = false)
        {
            bool returnResponse = false;

            if ((cancelled || tags == null) && (responseCode != (int)VipaSW1SW2Codes.CommandCancelled) &&
                (responseCode != (int)VipaSW1SW2Codes.UserEntryCancelled))
            {
                DeviceInteractionInformation?.TrySetResult((new LinkDALRequestIPA5Object(), responseCode));
                return;
            }

            LinkDALRequestIPA5Object cardResponse = new LinkDALRequestIPA5Object();

            foreach (TLV tag in tags)
            {
                if (tag.Tag == E0Template.E0TemplateTag)
                {
                    foreach (TLV dataTag in tag.InnerTags)
                    {
                        if (dataTag.Tag == E0Template.KeyPress)
                        {
                            cardResponse.DALResponseData = new LinkDALActionResponse
                            {
                                //Status = UserInteraction.UserKeyPressed.GetStringValue(),
                                Value = BCDConversion.StringFromByteData(dataTag.Data)
                            };
                            returnResponse = true;
                            break;
                        }
                    }

                    break;
                }
                else if (tag.Tag == E0Template.HTMLKeyPress)
                {
                    cardResponse.DALResponseData = new LinkDALActionResponse
                    {
                        Status = "UserKeyPressed",
                        Value = tag.Data[3] switch
                        {
                            // button actions as reported from HTML page
                            0x00 => DeviceKeys.KEY_2.ToString(),
                            0x1B => DeviceKeys.KEY_STOP.ToString(),
                            0x01 => DeviceKeys.KEY_1.ToString(),
                            0x0D => DeviceKeys.KEY_OK.ToString(),
                            _ => DeviceKeys.KEY_NONE.ToString()
                        }
                    };
                    returnResponse = true;
                    break;
                }
            }

            if (returnResponse)
            {
                DeviceInteractionInformation?.TrySetResult((cardResponse, responseCode));
            }
        }

        #endregion --- response handlers ---
    }
}
