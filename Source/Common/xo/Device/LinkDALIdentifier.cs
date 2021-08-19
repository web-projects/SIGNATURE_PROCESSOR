using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XO.Device
{
    public class LinkDALIdentifier
    {
    }

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
