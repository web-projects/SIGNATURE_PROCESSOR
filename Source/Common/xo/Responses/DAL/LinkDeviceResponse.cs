using System.Collections.Generic;
using XO.Common.DAL;

namespace XO.Responses.DAL
{
    public class LinkDeviceResponse
    {
        public List<LinkErrorValue> Errors { get; set; }

        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string FirmwareVersion { get; set; }
        public string TerminalId { get; set; }
        public string SerialNumber { get; set; }
        public string Port { get; set; }
        public List<string> Features { get; set; }
        public List<string> Configurations { get; set; }

        public LinkCardWorkflowControls CardWorkflowControls { get; set; }
    }
}
