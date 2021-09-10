
namespace Devices.Verifone.Connection.Interfaces
{
    public enum ReadingState { IncompleteData, CombiningData, Empty, ReadyToRead, ReadyToReadPlusMoreDataToConsume }

    public interface IVIPASerialParser
    {
        void BytesRead(byte[] chunk, int chunkLength = 0);

        void ReadAndExecute(VIPA.VIPAImpl.ResponseTagsHandlerDelegate responseTagsHandler, VIPA.VIPAImpl.ResponseTaglessHandlerDelegate responseTaglessHandler, VIPA.VIPAImpl.ResponseCLessHandlerDelegate responseContactlessHandler);

        bool SanityCheck();
    }
}
