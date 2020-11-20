using System;
using System.Collections.Generic;
using System.Text;
using XO.Responses;

namespace XO.Device
{
    public class LinkDeviceActionResponse
    {
        public string Status { get; set; }
        public List<LinkErrorValue> Errors { get; set; }
    }
}
