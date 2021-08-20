using Devices.Common;
using Devices.Verifone.VIPA;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Devices.Verifone.Connection
{
    public class SerialConnection : IDisposable, ISerialConnection
    {
        #region --- attributes ---
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

        private Thread readThread;
        private bool readContinue = true;
        private DeviceInformation deviceInformation;

        // monitor port change
        private bool connected;

        private const int portReadTimeout = 10000;
        private const int portWriteTimeout = 10000;
        private SerialPort serialPort;

        private readonly object ReadResponsesBytesLock = new object();
        private byte[] ReadResponsesBytes = Array.Empty<byte>();
        private List<byte[]> ReadResponseComponentBytes = new List<byte[]>();
        private ResponseBytesHandlerDelegate ResponseBytesHandler;
        public delegate void ResponseBytesHandlerDelegate(byte[] msg);

        private bool lastCDHolding;
        private string commPort;

        internal VIPA.VIPAImpl.ResponseTagsHandlerDelegate ResponseTagsHandler = null;
        internal VIPA.VIPAImpl.ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;
        internal VIPA.VIPAImpl.ResponseCLessHandlerDelegate ResponseContactlessHandler = null;

        public SerialConnection(DeviceInformation deviceInformation)
        {
            this.deviceInformation = deviceInformation;

            //if (deviceInformation.ComPort?.Length > 0 && !Config.SerialConfig.CommPortName.Equals(deviceInformation.ComPort, StringComparison.OrdinalIgnoreCase))
            //{
            //    Config.SerialConfig.CommPortName = deviceInformation.ComPort;
            //}
        }
        #endregion --- attributes ---

        #region --- private methods ---

        private void ReadResponses(byte[] responseBytes)
        {
            var validNADValues = new List<byte> { 0x01, 0x02, 0x11 };
            var validPCBValues = new List<byte> { 0x00, 0x01, 0x02, 0x03, 0x40, 0x41, 0x42, 0x43 };
            var nestedTagTags = new List<byte[]> { new byte[] { 0xEE }, new byte[] { 0xEF }, new byte[] { 0xF0 }, new byte[] { 0xE0 }, new byte[] { 0xE4 }, new byte[] { 0xE7 }, new byte[] { 0xFF, 0x7C }, new byte[] { 0xFF, 0x7F } };
            var powerManagement = new List<byte[]> { new byte[] { 0xE6 }, new byte[] { 0xC3 }, new byte[] { 0xC4 }, new byte[] { 0x9F, 0x1C } };
            var addedResponseComponent = false;

            lock (ReadResponsesBytesLock)
            {
                // Add current bytes to available bytes
                var combinedResponseBytes = new byte[ReadResponsesBytes.Length + responseBytes.Length];

                // TODO ---> @JonBianco BlockCopy should be leveraging here as it is vastly superior to Array.Copy
                // Combine prior bytes with new bytes
                Array.Copy(ReadResponsesBytes, 0, combinedResponseBytes, 0, ReadResponsesBytes.Length);
                Array.Copy(responseBytes, 0, combinedResponseBytes, ReadResponsesBytes.Length, responseBytes.Length);

                // Attempt to parse first message in response buffer
                var consumedResponseBytes = 0;
                var responseCode = 0;
                var errorFound = false;

                ReadErrorLevel readErrorLevel = ReadErrorLevel.None;

                // Validate NAD, PCB, and LEN values
                if (combinedResponseBytes.Length < 4)
                {
                    errorFound = true;
                    readErrorLevel = ReadErrorLevel.Length;
                }
                else if (!validNADValues.Contains(combinedResponseBytes[0]))
                {
                    errorFound = true;
                    readErrorLevel = ReadErrorLevel.Invalid_NAD;
                }
                else if (!validPCBValues.Contains(combinedResponseBytes[1]))
                {
                    errorFound = true;
                    readErrorLevel = ReadErrorLevel.Invalid_PCB;
                }
                else if (combinedResponseBytes[2] > (combinedResponseBytes.Length - 4))
                {
                    errorFound = true;
                    readErrorLevel = ReadErrorLevel.Invalid_CombinedBytes;
                }
                else
                {
                    // Validate LRC
                    byte lrc = 0;
                    var index = 0;
                    for (index = 0; index < (combinedResponseBytes[2] + 3); index++)
                    {
                        lrc ^= combinedResponseBytes[index];
                    }

                    if (combinedResponseBytes[combinedResponseBytes[2] + 3] != lrc)
                    {
                        errorFound = true;
                        readErrorLevel = ReadErrorLevel.Missing_LRC;
                    }
                    else if ((combinedResponseBytes[1] & 0x01) == 0x01)
                    {
                        var componentBytes = new byte[combinedResponseBytes[2]];
                        Array.Copy(combinedResponseBytes, 3, componentBytes, 0, combinedResponseBytes[2]);
                        ReadResponseComponentBytes.Add(componentBytes);
                        consumedResponseBytes = combinedResponseBytes[2] + 3 + 1;
                        errorFound = true;
                        readErrorLevel = ReadErrorLevel.CombinedBytes_MisMatch;
                        addedResponseComponent = true;
                    }
                    else
                    {
                        var sw1Offset = combinedResponseBytes[2] + 3 - 2;
                        //if ((combinedResponseBytes[sw1Offset] != 0x90) && (combinedResponseBytes[sw1Offset + 1] != 0x00))
                        //    errorFound = true;
                        responseCode = (combinedResponseBytes[sw1Offset] << 8) + combinedResponseBytes[sw1Offset + 1];
                    }
                }

                if (!errorFound)
                {
                    var totalDecodeSize = combinedResponseBytes[2] - 2;        // Use LEN of final response packet
                    foreach (var component in ReadResponseComponentBytes)
                    {
                        totalDecodeSize += component.Length;
                    }

                    var totalDecodeBytes = new byte[totalDecodeSize];
                    var totalDecodeOffset = 0;
                    foreach (var component in ReadResponseComponentBytes)
                    {
                        Array.Copy(component, 0, totalDecodeBytes, totalDecodeOffset, component.Length);
                        totalDecodeOffset += component.Length;
                    }
                    Array.Copy(combinedResponseBytes, 3, totalDecodeBytes, totalDecodeOffset, combinedResponseBytes[2] - 2);    // Skip final response header and use LEN of final response (no including the SW1, SW2, and LRC)

                    ReadResponseComponentBytes = new List<byte[]>();

                    if (ResponseTagsHandler != null || ResponseContactlessHandler != null)
                    {
                        TLV.TLV tlv = new TLV.TLV();
                        List<TLV.TLV> tags = null;

                        if (responseCode == (int)VipaSW1SW2Codes.Success)
                        {
                            tags = tlv.Decode(totalDecodeBytes, 0, totalDecodeBytes.Length, nestedTagTags);
                        }

                        //PrintTags(tags);
                        if (ResponseTagsHandler != null)
                        {
                            ResponseTagsHandler.Invoke(tags, responseCode);
                        }
                        else if (ResponseContactlessHandler != null)
                        {
                            ResponseContactlessHandler.Invoke(tags, responseCode, combinedResponseBytes[1]);
                        }
                    }
                    else if (ResponseTaglessHandler != null)
                    {
                        ResponseTaglessHandler.Invoke(totalDecodeBytes, responseCode);
                    }

                    consumedResponseBytes = combinedResponseBytes[2] + 3 + 1;  // Consumed NAD, PCB, LEN, [LEN] bytes, and LRC

                    addedResponseComponent = (combinedResponseBytes.Length - consumedResponseBytes) > 0;
                }
                else
                {
                    // allows for debugging of VIPA read issues
                    System.Diagnostics.Debug.WriteLine($"VIPA-READ: ON PORT={commPort} - ERROR LEVEL: '{readErrorLevel}'");
                }

                // Remove consumed bytes and leave remaining bytes for later consumption
                var remainingResponseBytes = new byte[combinedResponseBytes.Length - consumedResponseBytes];
                Array.Copy(combinedResponseBytes, consumedResponseBytes, remainingResponseBytes, 0, combinedResponseBytes.Length - consumedResponseBytes);

                ReadResponsesBytes = remainingResponseBytes;
            }

            if (addedResponseComponent)
            {
                ReadResponses(Array.Empty<byte>());
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        private void ReadResponseBytes()
        {
            while (readContinue)
            {
                try
                {
                    if (serialPort?.IsOpen ?? false)
                    { 
                        byte[] bytes = new byte[256];
                        var readLength = serialPort?.Read(bytes, 0, bytes.Length) ?? 0;
                        if (readLength > 0)
                        {
                            byte[] readBytes = new byte[readLength];
                            Array.Copy(bytes, 0, readBytes, 0, readLength);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine($"VIPA-READ [{serialPort?.PortName}]: {BitConverter.ToString(readBytes)}");
#endif
                            ResponseBytesHandler(readBytes);
                        }
                    }
                }
                catch (TimeoutException)
                {
                }
                // TODO: remove unnecessary catches after POC for multi-device is shakendown
                catch (InvalidOperationException)
                {
                }
                catch (OperationCanceledException)
                {
                }
                catch (NullReferenceException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        private void WriteBytes(byte[] msg)
        {
            try
            {
                serialPort?.Write(msg, 0, msg.Length);
            }
            catch (TimeoutException e)
            {
                Console.WriteLine($"SerialConnection: exception=[{e.Message}]");
            }
        }

        #endregion

        #region --- public methods ---

        public bool Connect(string port, bool exposeExceptions = false)
        {
            commPort = port;
            connected = false;

            try
            {
                // Create a new SerialPort object with default settings.
                serialPort = new SerialPort(commPort);

                // Update the Handshake
                serialPort.Handshake = Handshake.None;

                // Set the read/write timeouts
                serialPort.ReadTimeout = portReadTimeout;
                serialPort.WriteTimeout = portWriteTimeout;

                // open serial port
                serialPort.Open();

                // monitor port changes
                lastCDHolding = serialPort.CDHolding;

                // discard any buffered bytes
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();

                // Setup read thread
                readThread = new Thread(ReadResponseBytes);

                readThread.Start();
                ResponseBytesHandler += ReadResponses;

                Console.WriteLine($"SERIAL: ON PORT={commPort} - CONNECTION OPEN");

                connected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"SERIAL: ON PORT={commPort} - exception=[{e.Message}]");

                if (exposeExceptions)
                {
                    throw;
                }

                Dispose();
            }

            return connected;
        }

        public bool IsConnected()
        {
            return connected;
        }

        public void Disconnect(bool exposeExceptions = false)
        {
            if (serialPort?.IsOpen ?? false)
            {
                try
                {
                    readContinue = false;
                    connected = false;
                    Thread.Sleep(1000);

                    readThread.Join(1000);
                    ResponseBytesHandler -= ReadResponses;

                    // discard any buffered bytes
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();

                    serialPort.Close();

                    System.Diagnostics.Debug.WriteLine($"VIPA [{serialPort?.PortName}]: closed port.");
                }
                catch (Exception)
                {
                    if (exposeExceptions)
                    {
                        throw;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Disconnect();

            if (disposing)
            {
                serialPort?.Dispose();
                serialPort = null;
            }

            // https://docs.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.open?view=dotnet-plat-ext-3.1#System_IO_Ports_SerialPort_Open
            // SerialPort has a quirk (aka bug) where needs time to let a worker thread exit:
            //    "The best practice for any application is to wait for some amount of time after calling the Close method before
            //     attempting to call the Open method, as the port may not be closed instantly".
            // The amount of time is unspecified and unpredictable.
            Thread.Sleep(250);
        }

        #endregion

        #region --- COMMANDS ---

        public void WriteSingleCmd(VIPAResponseHandlers responsehandlers, VIPACommand command)
        {
            if (command == null)
            {
                return;
            }

            ResponseTagsHandler = responsehandlers.responsetagshandler;
            ResponseTaglessHandler = responsehandlers.responsetaglesshandler;
            ResponseContactlessHandler = responsehandlers.responsecontactlesshandler;

            int dataLen = command.data?.Length ?? 0;
            byte lrc = 0;

            if (0 < dataLen)
            {
                dataLen++;  // Allow for Lc byte
            }

            if (command.includeLE)
            {
                dataLen++;  // Allow for Le byte
            }

            var cmdLength = 7 /*NAD, PCB, LEN, CLA, INS, P1, P2*/ + dataLen + 1 /*LRC*/;
            var cmdBytes = new byte[cmdLength];
            var cmdIndex = 0;

            cmdBytes[cmdIndex++] = command.nad;
            lrc ^= command.nad;
            cmdBytes[cmdIndex++] = command.pcb;
            lrc ^= command.pcb;
            cmdBytes[cmdIndex++] = (byte)(4 /*CLA, INS, P1, P2*/ + dataLen /*Lc, data.Length, Le*/);
            lrc ^= (byte)(4 /*CLA, INS, P1, P2*/ + dataLen /*Lc, data.Length, Le*/);
            cmdBytes[cmdIndex++] = command.cla;
            lrc ^= command.cla;
            cmdBytes[cmdIndex++] = command.ins;
            lrc ^= command.ins;
            cmdBytes[cmdIndex++] = command.p1;
            lrc ^= command.p1;
            cmdBytes[cmdIndex++] = command.p2;
            lrc ^= command.p2;

            if (0 < command.data?.Length)
            {
                cmdBytes[cmdIndex++] = (byte)command.data.Length;
                lrc ^= (byte)command.data.Length;

                foreach (var byt in command.data)
                {
                    cmdBytes[cmdIndex++] = byt;
                    lrc ^= byt;
                }
            }

            if (command.includeLE)
            {
                cmdBytes[cmdIndex++] = command.le;
                lrc ^= command.le;
            }

            cmdBytes[cmdIndex++] = lrc;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"VIPA-WRITE: ON PORT={commPort} - {BitConverter.ToString(cmdBytes)}");
#endif
            WriteBytes(cmdBytes);
        }

        public void WriteRaw(byte []buffer)
        {
            System.Diagnostics.Debug.WriteLine($"VIPA-WRITE: ON PORT={commPort} - {BitConverter.ToString(buffer)}");
            WriteBytes(buffer);
        }

        #endregion --- COMMANDS ---
    }
}
