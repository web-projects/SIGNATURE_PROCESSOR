using Common.LoggerManager;
using Devices.Common;
using Devices.Verifone.Connection.Interfaces;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.TagLengthValue;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Devices.Verifone.Connection
{
    internal class VIPASerialParserImpl : IVIPASerialParser, IDisposable
    {
        #region --- Attributes ---

        private const int headerProtoLen = 4;   //  NAD, PCB, LEN and LRC
        private const int maxPacketLen = 254;
        private const int maxPacketProtoLen = maxPacketLen + headerProtoLen;

        private enum ReadErrorLevel
        {
            None,
            Length,
            Invalid_NAD,
            Invalid_PCB,
            Invalid_CombinedBytes,
            Missing_LRC,
            CombinedBytes_MisMatch
        }

        private DeviceLogHandler deviceLogHandler;

        private readonly ArrayPool<byte> arrayPool;
        private readonly List<Tuple<int, byte[]>> addedComponentBytes;
        private readonly object combinedResponseBytesLock = new object();

        private static readonly List<byte> validNADValues = new List<byte> { 0x01, 0x02, 0x11 };
        private static readonly List<byte> validPCBValues = new List<byte> { 0x00, 0x01, 0x02, 0x03, 0x40, 0x41, 0x42, 0x43 };
        private static readonly List<uint> nestedTagTags = new List<uint> { 0xEE, 0xEF, 0xF0, 0xE0, 0xE4, 0xE7, 0xFF7C, 0xFF7F };

        private byte[] combinedResponseBytes;
        private int combinedResponseLength;

        private readonly bool trackErrors;
        private readonly string comPort;
        private ReadErrorLevel readErrorLevel = ReadErrorLevel.None;

        private static ConcurrentDictionary<string, int> numReadErrors;

        #endregion ---Attributes ---

        public VIPASerialParserImpl(DeviceLogHandler deviceLogHandler, string comPort)
        {
            this.deviceLogHandler = deviceLogHandler;

            arrayPool = ArrayPool<byte>.Create();
            addedComponentBytes = new List<Tuple<int, byte[]>>();
            combinedResponseBytes = null;
            numReadErrors ??= new ConcurrentDictionary<string, int>();

            this.comPort = comPort;
            trackErrors = !string.IsNullOrWhiteSpace(comPort);
            if (trackErrors && !numReadErrors.ContainsKey(comPort))
            {
                numReadErrors[comPort] = 0;
            }
        }

        public void BytesRead(byte[] chunk, int chunkLength = 0)
        {
            if (chunk is null)
            {
                throw new ArgumentException(nameof(chunk));
            }
            if (chunkLength > chunk.Length)
            {
                throw new ArgumentException(nameof(chunkLength));
            }
            if (chunk.Length == 0)
            {
                return;
            }
            if (chunkLength <= 0)
            {
                chunkLength = chunk.Length;
            }

            lock (combinedResponseBytesLock)
            {
                if (combinedResponseLength + chunkLength > (combinedResponseBytes?.Length ?? 0))        //Expand current buffer to accomodate larger chunks
                {
                    byte[] tempArray = arrayPool.Rent(combinedResponseLength + chunkLength);

                    if (combinedResponseBytes is { })
                    {
                        Buffer.BlockCopy(combinedResponseBytes, 0, tempArray, 0, combinedResponseLength);
                        arrayPool.Return(combinedResponseBytes);
                    }

                    combinedResponseBytes = tempArray;
                }
            }
            Buffer.BlockCopy(chunk, 0, combinedResponseBytes, combinedResponseLength, chunkLength);
            combinedResponseLength += chunkLength;
        }

        private bool CheckForResponseErrors(ref bool addedResponseComponent, ref int consumedResponseBytesLength, ref int responseCode, bool isChainedMessageResponse)
        {
            bool isChainedCommand = (combinedResponseBytes[1] & 0x01) == 0x01;

            // Validate NAD, PCB, and LEN values
            if (combinedResponseLength < headerProtoLen)
            {
                readErrorLevel = ReadErrorLevel.Length;
                return true;
            }

            if (!isChainedMessageResponse)
            {
                if (!validNADValues.Contains(combinedResponseBytes[0]))
                {
                    readErrorLevel = ReadErrorLevel.Invalid_NAD;
                    return true;
                }
                else if (!validPCBValues.Contains(combinedResponseBytes[1]))
                {
                    readErrorLevel = ReadErrorLevel.Invalid_PCB;
                    return true;
                }
                //else if (combinedResponseBytes[2] > (combinedResponseLength - headerProtoLen))
                // FIX: CHAINED RESPONSE PROCESSING
                else if (combinedResponseBytes[2] > (combinedResponseLength - headerProtoLen) && !isChainedCommand)  // command is not chained
                {
                    readErrorLevel = ReadErrorLevel.Invalid_CombinedBytes;
                    return true;
                }
            }

            // FIX: CHAINED RESPONSE PROCESSING
            //int maxPacketLen = Math.Min((combinedResponseBytes[2] + 3), combinedResponseBytes.Length - 1);
            int maxPacketLen = isChainedCommand ? combinedResponseBytes.Length - 1 : combinedResponseBytes[2] + 3;

            // Validate LRC
            if (!isChainedMessageResponse)
            {
                byte lrc = CalculateLRCFromByteArray(combinedResponseBytes);

                // offset from length to LRC is 3
                //if (combinedResponseBytes[combinedResponseBytes[2] + 3] != lrc)
                // FIX: CHAINED RESPONSE PROCESSING
                if (!isChainedCommand && combinedResponseBytes[maxPacketLen] != lrc)
                {
                    readErrorLevel = ReadErrorLevel.Missing_LRC;
                    return true;
                }
            }

            //else if ((combinedResponseBytes[1] & 0x01) == 0x01)     //Command is chained (VIPA section 2.4)
            // FIX: CHAINED RESPONSE PROCESSING
            if (isChainedCommand || isChainedMessageResponse)  // Command is chained (VIPA section 2.4)
            {
                // obtain proper length from payload
                int length = combinedResponseBytes.Length - 1;
                for (; length > 0;)
                {
                    if (combinedResponseBytes[length] != 0x00)
                    {
                        break;
                    }

                    length--;
                }

                //int componentBytesLength = (int)Math.Max(combinedResponseBytes[2], length);
                int componentBytesLength = isChainedMessageResponse ? length : (int)combinedResponseBytes[2];
                byte[] componentBytes = arrayPool.Rent(componentBytesLength);

                // copy component bytes
                //Buffer.BlockCopy(combinedResponseBytes, 3, componentBytes, 0, componentBytesLength);
                Buffer.BlockCopy(combinedResponseBytes, 0, componentBytes, 0, componentBytesLength);

                addedComponentBytes.Add(new Tuple<int, byte[]>(componentBytesLength, componentBytes));

                // 1st packet      : NAD PCB(bit 0 set) LEN CLA INS P1 P2 Lc Data… LRC
                // 2nd – nth packet: NAD PCB(bit 0 set) LEN Data… LRC
                // Last packet     : NAD PCB(bit 0 unset) LEN Data… LRC
                consumedResponseBytesLength = componentBytesLength + headerProtoLen;
                readErrorLevel = ReadErrorLevel.CombinedBytes_MisMatch;
                addedResponseComponent = true;

                Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-RRCBADD [{comPort}]: {BitConverter.ToString(componentBytes, 0, componentBytesLength)}");

                if (isChainedMessageResponse)
                {
                    int sw1Offset = componentBytesLength - 2;
                    responseCode = (combinedResponseBytes[sw1Offset] << 8) + combinedResponseBytes[sw1Offset + 1];
                    readErrorLevel = ReadErrorLevel.None;
                }

                return !isChainedMessageResponse;
            }
            else
            {
                int sw1Offset = combinedResponseBytes[2] + 1;  // Offset to SW1 is forward 3, back 2 (back 1 for SW2)
                responseCode = (combinedResponseBytes[sw1Offset] << 8) + combinedResponseBytes[sw1Offset + 1];
                readErrorLevel = ReadErrorLevel.None;
            }

            return false;
        }

        public void ReadAndExecute(VIPAImpl.ResponseTagsHandlerDelegate responseTagsHandler, VIPAImpl.ResponseTaglessHandlerDelegate responseTaglessHandler, VIPAImpl.ResponseCLessHandlerDelegate responseContactlessHandler, bool isChainedMessageResponse = false)
        {
            bool addedResponseComponent = true;

            lock (combinedResponseBytesLock)
            {
                Debug.WriteLineIf(SerialConnection.LogSerialBytes && isChainedMessageResponse, $"VIPA-PARSE[{combinedResponseLength}]: " + BitConverter.ToString(combinedResponseBytes, 0, combinedResponseLength));
                if (isChainedMessageResponse)
                { 
                    Logger.debug($"{BitConverter.ToString(combinedResponseBytes, 0, combinedResponseLength).Replace("-", "")}");
                }

                while (addedResponseComponent && combinedResponseLength > 0 && combinedResponseBytes != null)
                {
                    int consumedResponseBytesLength = 0;
                    int responseCode = 0;
                    addedResponseComponent = false;

                    // Check for errors or extra responses.
                    bool errorFound = CheckForResponseErrors(ref addedResponseComponent, ref consumedResponseBytesLength, ref responseCode, isChainedMessageResponse);

                    if (!errorFound)
                    {
                        int totalDecodeSize = combinedResponseBytes[2] - 2;        // Use LEN of final response packet
                        foreach (Tuple<int, byte[]> component in addedComponentBytes)
                        {
                            totalDecodeSize += component.Item1;
                        }

                        byte[] totalDecodeBytes = arrayPool.Rent(isChainedMessageResponse ? combinedResponseLength : totalDecodeSize);
                        int totalDecodeOffset = 0;

                        foreach (Tuple<int, byte[]> component in addedComponentBytes)
                        {
                            Buffer.BlockCopy(component.Item2, 0, totalDecodeBytes, totalDecodeOffset, component.Item1);
                            totalDecodeOffset += component.Item1;
                            arrayPool.Return(component.Item2);
                        }

                        if (isChainedMessageResponse)
                        {
                            totalDecodeSize = combinedResponseLength - 3;
                            Buffer.BlockCopy(combinedResponseBytes, 3, totalDecodeBytes, 0, totalDecodeSize);
                        }
                        else
                        {
                            Buffer.BlockCopy(combinedResponseBytes, 3, totalDecodeBytes, totalDecodeOffset, combinedResponseBytes[2] - 2);    // Skip final response header and use LEN of final response (no including the SW1, SW2, and LRC)
                        }

                        addedComponentBytes.Clear();

                        if (responseTagsHandler != null || responseContactlessHandler != null)
                        {
                            List<TLV> tags = null;

                            if (responseCode == (int)VipaSW1SW2Codes.Success)
                            {
                                tags = TLV.Decode(totalDecodeBytes, 0, totalDecodeSize, nestedTagTags.ToArray());
                                Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-DECODED [{comPort}]: {BitConverter.ToString(totalDecodeBytes, 0, totalDecodeSize)}");
                            }

                            if (responseTagsHandler != null)
                            {
                                responseTagsHandler.Invoke(tags, responseCode);
                            }
                            else if (responseContactlessHandler != null)
                            {
                                responseContactlessHandler.Invoke(tags, responseCode, combinedResponseBytes[1]);
                            }
                        }
                        else if (responseTaglessHandler != null)
                        {
                            Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-TAGLESS DECODED [{comPort}]: {(responseCode == (int)VipaSW1SW2Codes.Success ? string.Empty : "NOTSUCCESS")} {BitConverter.ToString(totalDecodeBytes, 0, totalDecodeBytes.Length)}");
                            responseTaglessHandler.Invoke(totalDecodeBytes, totalDecodeSize, responseCode);
                        }
                        arrayPool.Return(totalDecodeBytes, false);

                        consumedResponseBytesLength = combinedResponseBytes[2] + headerProtoLen;

                        addedResponseComponent = (combinedResponseLength - consumedResponseBytesLength) > 0;
                    }
                    else if (readErrorLevel != ReadErrorLevel.CombinedBytes_MisMatch)
                    {
                        // allows for debugging of VIPA read issues
                        Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-READ [{comPort}]: ERROR LEVEL: '{readErrorLevel}'");
                        if (combinedResponseBytes is null || combinedResponseLength == 0)
                        {
                            //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"Error reading vipa-byte stream({readErrorLevel}): 0 || <null>");
                            Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"Error reading vipa-byte stream({readErrorLevel}");
                        }
                        else
                        {
                            //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"Error reading vipa-byte stream({readErrorLevel}): " + BitConverter.ToString(combinedResponseBytes, 0, combinedResponseLength));
                            Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"Error reading vipa-byte stream({readErrorLevel}): " + BitConverter.ToString(combinedResponseBytes, 0, combinedResponseLength));
                        }
                    }

                    if (consumedResponseBytesLength >= combinedResponseLength)
                    {
                        // All bytes consumed.  Leave a null array for later
                        if (combinedResponseBytes is { })
                        {
                            arrayPool.Return(combinedResponseBytes, false);
                            combinedResponseBytes = null;
                            combinedResponseLength = 0;
                        }
                    }
                    else if (consumedResponseBytesLength > 0)
                    {
                        // Remove consumed bytes and leave remaining bytes for later consumption
                        int updatedLength = combinedResponseLength - consumedResponseBytesLength;
                        byte[] tempArray = arrayPool.Rent(updatedLength);
                        Buffer.BlockCopy(combinedResponseBytes, consumedResponseBytesLength, tempArray, 0, updatedLength);
                        arrayPool.Return(combinedResponseBytes, false);
                        combinedResponseBytes = tempArray;
                        combinedResponseLength = updatedLength;
                    }
                }
            }
        }

        public bool SanityCheck()
        {
            Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheck in progress....");

            bool sane = true;

            if (combinedResponseLength > 0 || combinedResponseBytes is { })
            {
                sane = false;
                if (combinedResponseBytes is { })
                {
                    //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"VIPA-PARSE[{comPort}]: SanityCheckFailed-{combinedResponseLength}-{BitConverter.ToString(combinedResponseBytes, 0, combinedResponseBytes.Length)}");
                    Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheckFailed-LEN={combinedResponseLength}\r\nBYTES: [{BitConverter.ToString(combinedResponseBytes, 0, combinedResponseBytes.Length)}]");
                    arrayPool.Return(combinedResponseBytes);
                    combinedResponseBytes = null;
                }
                else
                {
                    //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"VIPA-PARSE[{comPort}]: SanityCheckFailed-{combinedResponseLength}");
                    Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheckFailed-LEN={combinedResponseLength}");
                    combinedResponseLength = 0;
                }
            }

            // chained command answer: component bytes should be assembled into a single packet
            if (addedComponentBytes.Count > 0)
            {
                sane = false;
                //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"VIPA-PARSE[{comPort}]: SanityCheckFailedComponentCheck-{addedComponentBytes.Count}");
                Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheckFailedComponentCheck-LEN={addedComponentBytes.Count}");
                foreach (Tuple<int, byte[]> component in addedComponentBytes)
                {
                    //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"VIPA-PARSE[{comPort}]: SanityCheckFailed-StoredComponent-{BitConverter.ToString(component.Item2, 0, component.Item1)}");
                    Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheckFailed-StoredComponent\r\nBYTES: [{BitConverter.ToString(component.Item2, 0, component.Item1)}]");
                    arrayPool.Return(component.Item2);
                }
                addedComponentBytes.Clear();
            }

            if (ReadErrorLevel.None != readErrorLevel)
            {
                sane = false;
                //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Warn, $"VIPA-PARSE[{comPort}]: SanityCheckFailedStateCheck-{readErrorLevel}");
                Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheckFailedStateCheck-{readErrorLevel}");
            }

            Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-PARSE[{comPort}]: SanityCheck complete - STATUS={sane}");

            return sane;
        }

        public void Dispose()
        {
            if (combinedResponseBytes is { })
            {
                arrayPool.Return(combinedResponseBytes);
            }
        }

        private byte CalculateLRCFromByteArray(byte[] array)
        {
            // VIPA Specification: the maximum possible LEN byte value is 0xFE (254 bytes)
            //int maxPacketLen = Math.Min((array[2] + 3), (0xFE - 0x01));
            int maxPacketLen = Math.Min((array[2] + 3), array.Length - 1);
            byte lrc = 0x00;

            //for (int index = 0; index < (array[2] + 3); index++)
            // FIX: CHAINED RESPONSE PROCESSING
            for (int index = 0; index < maxPacketLen; index++)
            {
                lrc ^= array[index];
            }

            return lrc;
        }
    }
}
