using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;

namespace Devices.Simulator.Connection
{
    public class SerialConnection : ISerialConnection, IDisposable
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

        private SerialPort serialPort;

        private readonly Object ReadResponsesBytesLock = new object();
        private byte[] ReadResponsesBytes = Array.Empty<byte>();
        private List<byte[]> ReadResponseComponentBytes = new List<byte[]>();
        private ResponseBytesHandlerDelegate ResponseBytesHandler;
        public delegate void ResponseBytesHandlerDelegate(byte[] msg);

        private bool lastCDHolding;
        private bool connected;
        private string commPort;

        #endregion --- attributes ---

        #region --- private methods ---

        private void ReadResponses(byte[] responseBytes)
        {

        }

        [System.Diagnostics.DebuggerNonUserCode]
        private void ReadResponseBytes()
        {
            while (readContinue)
            {
                try
                {
                    byte[] bytes = new byte[256];
                    var readLength = serialPort.Read(bytes, 0, bytes.Length);
                    if (readLength > 0)
                    {
                        byte[] readBytes = new byte[readLength];
                        Array.Copy(bytes, 0, readBytes, 0, readLength);

                        ResponseBytesHandler(readBytes);
#if DEBUG
                        Console.WriteLine($"DEVICE-READ: {BitConverter.ToString(readBytes)}");
                        System.Diagnostics.Debug.WriteLine($"READ: {BitConverter.ToString(readBytes)}");
#endif
                    }
                }
                catch (TimeoutException)
                {
                }
                catch (Exception)
                {
                }
            }
        }

        private void WriteBytes(byte[] msg)
        {
            try
            {
                serialPort.Write(msg, 0, msg.Length);
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
                // Setup read thread
                readThread = new Thread(ReadResponseBytes);

                // Create a new SerialPort object with default settings.
                serialPort = new SerialPort(commPort);

                // Update the Handshake
                serialPort.Handshake = Handshake.None;

                // Set the read/write timeouts
                serialPort.ReadTimeout = 10000;
                serialPort.WriteTimeout = 10000;

                // open serial port
                serialPort.Open();

                connected = serialPort.IsOpen;

                if (connected)
                {
                    // monitor port changes
                    //PortsChanged += OnPortsChanged;
                    lastCDHolding = serialPort.CDHolding;

                    // discard any buffered bytes
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();

                    ResponseBytesHandler += ReadResponses;

                    readThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"SerialConnection: exception=[{e.Message}]");

                if (exposeExceptions)
                {
                    throw;
                }
            }

            return connected;
        }

        public bool Connected()
        {
            try
            {
                if (lastCDHolding != serialPort?.CDHolding)
                {
                    connected = false;
                    Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"SerialConnection: exception=[{e.Message}]");
            }

            return connected;
        }

        public void Disconnect(bool exposeExceptions = false)
        {
            if (serialPort?.IsOpen ?? false)
            {
                try
                {
                    readContinue = false;
                    Thread.Sleep(1000);

                    readThread.Join(1024);
                    ResponseBytesHandler -= ReadResponses;

                    // discard any buffered bytes
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();

                    serialPort.Close();
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
            try
            {
                readContinue = false;
                Thread.Sleep(100);
                Disconnect();
            }
            finally
            {
                Dispose(true);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            Stream internalSerialStream = (Stream)serialPort.GetType()
                .GetField("internalSerialStream", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(serialPort);

            GC.SuppressFinalize(serialPort);
            GC.SuppressFinalize(internalSerialStream);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region --- COMMANDS ---

        public void WriteSingleCmd()
        {
            //WriteBytes(cmdBytes);
        }

        #endregion --- COMMANDS ---
    }
}
