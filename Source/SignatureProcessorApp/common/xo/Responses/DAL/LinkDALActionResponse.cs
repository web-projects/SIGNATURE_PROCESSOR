namespace SignatureProcessorApp.common.xo.Responses.DAL
{
    public class LinkDALActionResponse
    {
        //public List<LinkErrorValue> Errors { get; set; }
        public string Status { get; set; }
        public string Value { get; set; }
        public bool? CardPresented { get; set; }
        public bool? CardRemoved { get; set; }
    }
}
