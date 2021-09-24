using Common.LoggerManager;
using Devices.Common;
using Devices.Verifone.Connection.Interfaces;
using Devices.Verifone.VIPA;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Verifone.Connection
{
    public class SerialConnection : IDisposable
    {
#if DEBUG
        internal const bool LogSerialBytes = true;
#else
        internal const bool LogSerialBytes = false;
#endif
        internal VIPAImpl.ResponseTagsHandlerDelegate ResponseTagsHandler = null;
        internal VIPAImpl.ResponseTaglessHandlerDelegate ResponseTaglessHandler = null;
        internal VIPAImpl.ResponseCLessHandlerDelegate ResponseContactlessHandler = null;

        // optimize serial port read buffer size based on expected response
        private const int unchainedResponseMessageSize = 1024;
        private const int chainedResponseMessageSize = unchainedResponseMessageSize * 10;

        private CancellationTokenSource cancellationTokenSource;
        private SerialPort serialPort;
        private readonly IVIPASerialParser serialParser;
        private bool readingSerialPort = false;
        private bool shouldStopReading;
        private bool readerThreadIsActive;
        private bool disposedValue;
        private readonly ArrayPool<byte> arrayPool;
        private readonly object readerThreadLock = new object();

        public bool IsChainedMessageResponse { get; set; }

        // TODO: Dependency should be injected.
        internal DeviceConfig Config { get; } = new DeviceConfig().SetSerialDeviceConfig(new Common.SerialDeviceConfig());

        public SerialConnection(DeviceInformation deviceInformation, DeviceLogHandler deviceLogHandler)
        {
            serialParser = new VIPASerialParserImpl(deviceLogHandler, deviceInformation.ComPort);
            cancellationTokenSource = new CancellationTokenSource();
            arrayPool = ArrayPool<byte>.Create();

            if (deviceInformation.ComPort?.Length > 0 && !Config.SerialConfig.CommPortName.Equals(deviceInformation.ComPort, StringComparison.OrdinalIgnoreCase))
            {
                Config.SerialConfig.CommPortName = deviceInformation.ComPort;
            }
        }

        public bool Connect(bool exposeExceptions = false)
        {
            bool connected = false;

            try
            {
                // Create a new SerialPort object with default settings.
                serialPort = new SerialPort(Config.SerialConfig.CommPortName, Config.SerialConfig.CommBaudRate, Config.SerialConfig.CommParity,
                    Config.SerialConfig.CommDataBits, Config.SerialConfig.CommStopBits);

                // Update the Handshake
                serialPort.Handshake = Config.SerialConfig.CommHandshake;

                // Set the read/write timeouts
                serialPort.ReadTimeout = Config.SerialConfig.CommReadTimeout;
                serialPort.WriteTimeout = Config.SerialConfig.CommWriteTimeout;
                serialPort.DataReceived += SerialPort_DataReceived;

                serialPort.Open();

                // discard any buffered bytes
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();

                connected = true;
                shouldStopReading = false;
                readerThreadIsActive = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VIPA [{serialPort?.PortName}]: {ex.Message}");

                if (exposeExceptions)
                {
                    throw;
                }

                Dispose();
            }

            return connected;
        }

        public bool IsConnected() => serialPort?.IsOpen ?? false;

        public void Disconnect(bool exposeExceptions = false)
        {
            Debug.WriteLine($"VIPA [{serialPort?.PortName ?? "COMXX"}]: disconnect request.");

            shouldStopReading = true;

            try
            {
                cancellationTokenSource?.Cancel();

                // discard any buffered bytes
                if (serialPort?.IsOpen ?? false)
                {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();

                    serialPort.Close();

                    Debug.WriteLine($"VIPA [{serialPort?.PortName}]: closed port.");
                }
            }
            catch (Exception)
            {
                if (exposeExceptions)
                {
                    throw;
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();
                    serialPort?.Dispose();
                    serialPort = null;
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                }
                disposedValue = true;

                // https://docs.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.open?view=dotnet-plat-ext-3.1#System_IO_Ports_SerialPort_Open
                // SerialPort has a quirk (aka bug) where needs time to let a worker thread exit:
                //    "The best practice for any application is to wait for some amount of time after calling the Close method before
                //     attempting to call the Open method, as the port may not be closed instantly".
                // The amount of time is unspecified and unpredictable.
                Thread.Sleep(250);
            }
        }

        private bool IsChainedResponseCommand(VIPACommand command) =>
            (VIPACommandType)(command.cla << 8 | command.ins) == VIPACommandType.DisplayHTML && command.data != null &&
            Encoding.UTF8.GetString(command.data).IndexOf(VIPACommand.ChainedResponseAnswerData, StringComparison.OrdinalIgnoreCase) >= 0;

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

            int cmdLength = 7 /*NAD, PCB, LEN, CLA, INS, P1, P2*/ + dataLen + 1 /*LRC*/;
            byte[] cmdBytes = arrayPool.Rent(cmdLength);
            int cmdIndex = 0;

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

                foreach (byte byt in command.data)
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

            // chained message response
            IsChainedMessageResponse = IsChainedResponseCommand(command);

            Debug.WriteLineIf(LogSerialBytes, $"VIPA-WRITE[{serialPort?.PortName}]: {BitConverter.ToString(cmdBytes)}");
            WriteBytes(cmdBytes, cmdLength);

            arrayPool.Return(cmdBytes);
        }

        public void WriteRaw(byte[] buffer, int length)
        {
            Debug.WriteLineIf(LogSerialBytes, $"VIPA-WRITE: ON PORT={serialPort?.PortName} - {BitConverter.ToString(buffer)}");
            WriteBytes(buffer, length);
        }

        [DebuggerNonUserCode]
        private async Task ReadExistingResponseBytes()
        {
            while (!shouldStopReading)
            {
                if (!readingSerialPort && !IsChainedMessageResponse)
                {
                    await Task.Delay(100);
                    continue;
                }

                byte[] buffer = arrayPool.Rent(unchainedResponseMessageSize);  //Read the whole thing if possible.

                bool moreData = serialPort?.IsOpen ?? false;

                while (moreData && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (serialPort.BytesToRead > 0)
                        {
                            bool parseBytes = true;

                            int readLength = serialPort.Read(buffer, 0, buffer.Length);
                            Debug.WriteLineIf(LogSerialBytes && !IsChainedMessageResponse, $"VIPA-READ [{serialPort.PortName}]: {BitConverter.ToString(buffer, 0, readLength)}");

                            if (IsChainedMessageResponse)
                            {
                                //Logger.debug($"{BitConverter.ToString(buffer, 0, readLength).Replace("-", "")}");

                                // SW1-SW2-LRC in trailing edge of data frame
                                if (buffer[readLength - 3] == 0x90 && buffer[readLength - 2] == 0x00)
                                {
                                    // setup chained-message-response buffer after chained-command response
                                    if (buffer[1] == 0x00)
                                    {
                                        // chained command answer: expect SW1SW2=0x9000
                                        serialParser.BytesRead(buffer, readLength);
                                        serialParser.ReadAndExecute(ResponseTagsHandler, ResponseTaglessHandler, ResponseContactlessHandler);
                                        serialParser.SanityCheck();
                                        parseBytes = false;
                                        // grow the buffer as signature payload is large
                                        arrayPool.Return(buffer);
                                        buffer = arrayPool.Rent(chainedResponseMessageSize);
                                    }
                                    else
                                    {
                                        moreData = false;
                                    }
                                }
                            }

                            // assemble combined bytes for chained answer response
                            if (parseBytes)
                            {
                                serialParser.BytesRead(buffer, readLength);
                            }
                        }
                        else if (!IsChainedMessageResponse)
                        {
                            moreData = false;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // This is acceptable as the SerialPort library might timeout and recover
                        moreData = false;
                        Debug.WriteLine($"TimedOut VIPA-READ [{serialPort.PortName}]");
                    }
                    // TODO: remove unnecessary catches after POC for multi-device is shakendown
                    catch (InvalidOperationException ioe)
                    {
                        moreData = false;
                        Debug.WriteLine($"Invalid Operation VIPA-READ [{serialPort.PortName}]: {ioe.Message}");
                    }
                    catch (OperationCanceledException oce)
                    {
                        moreData = false;
                        Debug.WriteLine($"Operation Cancelled VIPA-READ [{serialPort.PortName}]: {oce.Message}");
                    }
                    catch (Exception ex)
                    {
                        moreData = false;
                        Debug.WriteLine($"Exception VIPA-READ [{serialPort.PortName}]: {ex.Message}");
                    }
                    finally
                    {
                        arrayPool.Return(buffer);
                    }
                }

                readingSerialPort = false;

                if (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    serialParser.ReadAndExecute(ResponseTagsHandler, ResponseTaglessHandler, ResponseContactlessHandler, IsChainedMessageResponse);
                    serialParser.SanityCheck();
                }
            }

            readerThreadIsActive = false;
        }

        private void WriteBytes(byte[] msg, int cmdLength)
        {
            try
            {
                serialPort?.Write(msg, 0, cmdLength);
            }
            catch (TimeoutException)
            {
                //We aren't worried about timeouts.  All other exceptions we should allow to throw
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!readingSerialPort)
            {
                readingSerialPort = true;

                if (!readerThreadIsActive)
                {
                    lock (readerThreadLock)
                    {
                        if (!readerThreadIsActive)
                        {
                            readerThreadIsActive = true;
                            Task.Run(ReadExistingResponseBytes, cancellationTokenSource.Token);
                        }
                    }
                }
            }
        }
    }
}
