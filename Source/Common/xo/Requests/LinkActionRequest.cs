using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XO.Requests.DAL;

namespace XO.Requests
{
    public class LinkActionRequest
    {
        public string MessageID { get; set; }

        public int Timeout { get; set; }

        public LinkAction? Action { get; set; }
        
        public LinkDeviceRequest DeviceRequest { get; set; }
        
        public LinkDeviceActionRequest DeviceActionRequest { get; set; }

        public LinkDALRequest DALRequest { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkAction
    {
        Payment,
        ActionStatus,
        PaymentUpdate,
        DALAction,
        EstablishProxy,
        Report,
        Session
    }
}
