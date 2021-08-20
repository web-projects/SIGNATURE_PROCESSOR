namespace Devices.Verifone.VIPA
{
    public class VIPAResponseHandlers
    {
        public VIPAImpl.ResponseTagsHandlerDelegate responsetagshandler { get; set; }
        public VIPAImpl.ResponseTaglessHandlerDelegate responsetaglesshandler { get; set; }

        public VIPAImpl.ResponseCLessHandlerDelegate responsecontactlesshandler { get; set; }
    }
}