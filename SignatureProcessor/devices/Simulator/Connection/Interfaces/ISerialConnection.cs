namespace Devices.Simulator.Connection
{
    internal interface ISerialConnection
    {
        bool Connect(string port, bool exposeExceptions = false);
        bool Connected();
        void Disconnect(bool exposeExceptions = false);
        void Dispose();
        void WriteSingleCmd();
    }
}