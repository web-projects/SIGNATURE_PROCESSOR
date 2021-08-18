using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Devices.Common.Helpers
{
    public class EventResponses
    {
        //Event Code
        [JsonConverter(typeof(StringEnumConverter))]
        public enum EventCodeType
        {
            DEVICE_PLUGGED,
            DEVICE_UNPLUGGED,
            CANCEL_KEY_PRESSED,
            USER_CANCELED,
            REQUEST_TIMEOUT,
            CANCELLATION_REQUEST
        }
    }
}
