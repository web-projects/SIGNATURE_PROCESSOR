﻿using Devices.Common;
using Devices.Common.Helpers;
using Devices.Verifone.Connection.Interfaces;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.TagLengthValue;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
                else if (combinedResponseBytes[2] > (combinedResponseLength - headerProtoLen) && !isChainedCommand)  // command is not chained
                {
                    readErrorLevel = ReadErrorLevel.Invalid_CombinedBytes;
                    return true;
                }
            }

            int maxPacketLen = isChainedCommand ? combinedResponseBytes.Length - 1 : combinedResponseBytes[2] + 3;

            if (!isChainedMessageResponse)
            {
                // Validate LRC
                byte lrc = CalculateLRCFromByteArray(combinedResponseBytes);

                // offset from length to LRC is 3
                if (!isChainedCommand && combinedResponseBytes[maxPacketLen] != lrc)
                {
                    readErrorLevel = ReadErrorLevel.Missing_LRC;
                    return true;
                }
            }

            if (isChainedMessageResponse || isChainedCommand)  // Command is chained (VIPA section 2.4)
            {
                // reassemble chained message response
                if (isChainedMessageResponse)
                {
                    if (ProcessChainedMessageResponse())
                    {
                        return true;
                    }
                }
                else
                {
                    int componentBytesLength = (int)combinedResponseBytes[2];
                    byte[] componentBytes = arrayPool.Rent(componentBytesLength);

                    // copy component bytes
                    Buffer.BlockCopy(combinedResponseBytes, 3, componentBytes, 0, componentBytesLength);

                    addedComponentBytes.Add(new Tuple<int, byte[]>(componentBytesLength, componentBytes));
                    consumedResponseBytesLength = componentBytesLength + headerProtoLen;

                    Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-RRCBADD [{comPort}]: {BitConverter.ToString(componentBytes, 0, componentBytesLength)}");

                    string resultString = Regex.Replace(ConversionHelper.ByteArrayCodedHextoString(componentBytes), @"[^\u0020-\u007E]", ".", RegexOptions.Compiled);
                    Debug.WriteLine($"CFRE:\r\n'{resultString}'");
                }

                // 1st packet      : NAD PCB(bit 0 set) LEN CLA INS P1 P2 Lc Data… LRC
                // 2nd – nth packet: NAD PCB(bit 0 set) LEN Data… LRC
                // Last packet     : NAD PCB(bit 0 unset) LEN Data… LRC
                readErrorLevel = ReadErrorLevel.CombinedBytes_MisMatch;
                addedResponseComponent = true;

                if (isChainedMessageResponse)
                {
                    int componentBytesLength = CalculateByteArrayLength(combinedResponseBytes, combinedResponseLength - 1);
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
                if (combinedResponseLength > 0)
                {
                    Debug.WriteLineIf((SerialConnection.LogSerialBytes && isChainedMessageResponse), $"VIPA-PARSE[{combinedResponseLength}]: " + BitConverter.ToString(combinedResponseBytes, 0, combinedResponseLength));
                }
                //if (isChainedMessageResponse)
                //{
                //    Logger.debug($"{BitConverter.ToString(combinedResponseBytes, 0, combinedResponseLength).Replace("-", "")}");
                //}

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

                        byte[] totalDecodeBytes = arrayPool.Rent(totalDecodeSize);
                        Array.Clear(totalDecodeBytes, 0, totalDecodeBytes.Length);

                        int totalDecodeOffset = 0;
                        int frame = 1;

                        // assemble totalDecodeBytes with component payload
                        foreach (Tuple<int, byte[]> component in addedComponentBytes)
                        {
                            Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-RRCBADD [{comPort}]|FRAME#{frame++}: {BitConverter.ToString(component.Item2, 0, component.Item1)}");

                            Buffer.BlockCopy(component.Item2, 0, totalDecodeBytes, totalDecodeOffset, component.Item1);
                            totalDecodeOffset += component.Item1;
                            arrayPool.Return(component.Item2);
                        }

                        string resultString = Regex.Replace(ConversionHelper.ByteArrayCodedHextoString(totalDecodeBytes), @"[^\u0020-\u007E]", ".", RegexOptions.Compiled);
                        Debug.WriteLine($"RAE-1:\r\n'{resultString.Replace(".", string.Empty)}'");

                        if (isChainedMessageResponse)
                        {
                            totalDecodeSize = totalDecodeOffset - 2; // skip final response header
                            totalDecodeSize = CalculateByteArrayLength(totalDecodeBytes, totalDecodeSize - 1);
                            consumedResponseBytesLength = combinedResponseLength = totalDecodeSize;
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

                    if (consumedResponseBytesLength >= combinedResponseLength || isChainedMessageResponse)
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

        private void PoolReturnIfNotNull(ArrayPool<byte> pool, byte[] buffer)
        {
            if (buffer != null)
            {
                pool.Return(buffer);
            }
        }

        private bool ProcessChainedMessageResponse()
        {
            ArrayPool<byte> workerPool = ArrayPool<byte>.Create();

            byte[] workerBuffer = null;
            byte[] componentBytes = null;

            try
            {
                // obtain proper length from payload
                int messageLength = CalculateByteArrayLength(combinedResponseBytes, combinedResponseLength - 1);

                componentBytes = arrayPool.Rent(messageLength);

                int offset = 0;
                int frame = 1;

                // VIPA Specification: the maximum possible LEN byte value is 0xFE (254 bytes) + headerProtoLen
                for (int i = 0; i < messageLength; i += maxPacketProtoLen)
                {
                    // copy MAX of 258 block sizes (NAD+PCB+LEN+LRC+FE_BYTES_DATA_MAX) or LESS depending on last packet with PCB = 0
                    int blockCopyLength = CalculateByteArrayLength(combinedResponseBytes, i + maxPacketProtoLen - 1) - i + 1;
                    workerBuffer = workerPool.Rent(blockCopyLength);
                    Buffer.BlockCopy(combinedResponseBytes, i, workerBuffer, 0, blockCopyLength);

                    // assume the buffer length is correct
                    int workerBufferLen = workerBuffer[2] + headerProtoLen;

                    // last message in chained response PCB bit is set to 0: total length = LEN + SW1 + SW2 + LRC
                    byte lrc = CalculateLRCFromByteArray(workerBuffer, (workerBuffer[1] == 0x00 ? workerBuffer[2] + 0x03 : 0));
                    workerBufferLen = (workerBuffer[1] == 0x00) ? workerBuffer[2] + 0x03 : workerBufferLen - 1;

                    if (workerBuffer[workerBufferLen] != lrc)
                    {
                        Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-RRCBADD [{comPort}]|FRAME#{frame++}: {BitConverter.ToString(workerBuffer, 0, workerBufferLen)}");
                        workerPool.Return(workerBuffer);
                        readErrorLevel = ReadErrorLevel.Missing_LRC;
                        return true;
                    }

                    // remove LRC
                    workerBufferLen -= i > 0 ? 3 : 0;
                    Buffer.BlockCopy(workerBuffer, ((i > 0) ? 3 : 0), componentBytes, offset, workerBufferLen);
                    offset += workerBufferLen;

                    Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"VIPA-RRCBADD [{comPort}]|FRAME#{frame++}: {BitConverter.ToString(workerBuffer, 0, workerBufferLen)}");

                    workerPool.Return(workerBuffer);
                }

                int componentBytesLength = offset;
                addedComponentBytes.Add(new Tuple<int, byte[]>(componentBytesLength, componentBytes));
            }
            catch (Exception ex)
            {
                //deviceLogHandler?.Invoke(XO.ProtoBuf.LogMessage.Types.LogLevel.Error, $"Error processing chained-message response({ex.Message})");
                Debug.WriteLineIf(SerialConnection.LogSerialBytes, $"Error processing chained-message response({ex.Message})");
                PoolReturnIfNotNull(workerPool, workerBuffer);
                PoolReturnIfNotNull(arrayPool, componentBytes);
            }

            return false;
        }

        private int CalculateByteArrayLength(byte[] array, int startPosition)
        {
            // array length returns the size of the array instead of the length of its contents
            int length;

            // check for possible buffer overrun on last copied block
            for (length = Math.Min(startPosition, array.Length - 1); length > 0; length--)
            {
                if (array[length] != 0x00)
                {
                    break;
                }
            }
            return length;
        }

        private byte CalculateLRCFromByteArray(byte[] array, int packetOffset = 0)
        {
            // VIPA Specification: the maximum possible LEN byte value is 0xFE (254 bytes)
            int maxPacketLen = Math.Min((packetOffset > 0 ? packetOffset : (array[2] + 3)), array.Length - 1);
            byte lrc = 0x00;

            for (int index = 0; index < maxPacketLen; index++)
            {
                lrc ^= array[index];
            }

            return lrc;
        }
    }
}
