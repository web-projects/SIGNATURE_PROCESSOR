using System;
using System.Collections.Generic;
using System.Text;
using XO.Device;

namespace XO.Requests
{
    public class LinkDeviceRequest
    {
        public LinkDALIdentifier DALIdentifier { get; set; }
        public LinkDeviceIdentifier DeviceIdentifier { get; set; }
    }
}
