using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XO.Requests
{
    public partial class LinkDeviceActionRequest
    {
        public LinkDeviceActionType? DeviceAction { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkDeviceActionType
    {
        Configuration,
        GetStatus,
        AbortCommand,
        ResetCommand,
        RebootDevice,
        GetIdentifier,
        GetActiveKeySlot,
        GetEMVKernelChecksum,
        GetSecurityConfiguration,
        FeatureEnablementToken,
        LockDeviceConfig0,
        LockDeviceConfig8,
        UnlockDeviceConfig,
        UpdateHMACKeys,
        GenerateHMAC
    }
}
