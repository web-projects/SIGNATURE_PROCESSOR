using Devices.Verifone.VIPA;
using XO.Private;
using XO.Responses;

namespace Devices.Verifone.Helpers
{
    public class DeviceInfoObject
    {
        public VipaSW1SW2Codes Status { get; set; }
        public LinkDeviceResponse LinkDeviceResponse { get; set; }
        public LinkDALRequestIPA5Object LinkDALRequestIPA5Object { get; set; }
    }
}
