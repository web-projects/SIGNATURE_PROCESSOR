namespace Devices.Verifone.VIPA
{
    public class VIPAResponseHandlers
    {
        public VIPA.ResponseTagsHandlerDelegate responsetagshandler { get; set; }
        public VIPA.ResponseTaglessHandlerDelegate responsetaglesshandler { get; set; }

        public VIPA.ResponseCLessHandlerDelegate responsecontactlesshandler { get; set; }
    }
}