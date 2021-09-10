using XO.Device;
using XO.Private;

namespace XO.Requests.DAL
{
    public class LinkDALRequest
    {
        public LinkDALIdentifier DALIdentifier { get; set; }
        public LinkDALRequestIPA5Object LinkObjects { get; set; }
        public LinkDeviceIdentifier DeviceIdentifier { get; set; }
    }
}
