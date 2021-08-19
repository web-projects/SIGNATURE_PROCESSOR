using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XO.Common.DAL
{
    public class LinkDALIdentifier
    {
        public string WorkstationName { get; set; }
        public string DnsName { get; set; }
        public string IPv4 { get; set; }
        public string IPv6 { get; set; }
        public string Username { get; set; }
        public LinkDALLookupPreference? LookupPreference { get; set; }
        public string Version { get; set; }
    }

    //DAL lookup preference selection
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDALLookupPreference
    {
        WorkstationName,
        DnsName,
        IPv4,
        IPv6,
        Username
    }
}
