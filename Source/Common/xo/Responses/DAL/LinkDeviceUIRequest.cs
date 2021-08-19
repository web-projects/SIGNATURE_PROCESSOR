using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace XO.Responses.DAL
{
    public class LinkDeviceUIRequest
    {
        public LinkDeviceUIActionType? UIAction { get; set; }
        public List<string> DisplayText { get; set; }
        public int DisplayMessageIndex { get; set; }
        public bool ResetDeviceEMVCollectionData { get; set; }
    }

    //DeviceUI action selection
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDeviceUIActionType
    {
        DisplayIdle,
        KeyRequest,
        InputRequest,
        Display
    }
}
