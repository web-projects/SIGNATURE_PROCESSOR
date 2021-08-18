using static System.ExtensionMethods;

namespace SignatureProcessorApp.devices.Verifone.VIPA.Helpers
{
    public enum DeviceKeys
    {
        [StringValue("KEY_NONE")]
        KEY_NONE = 0xFF,
        [StringValue("KEY_GREEN")]
        KEY_OK = 0x0D,
        [StringValue("KEY_RED")]
        KEY_STOP = 0x1B,
        [StringValue("KEY_CORR")]
        KEY_CORR = 0x08,
        [StringValue("KEY_UP")]
        KEY_UP = 0x86,
        [StringValue("KEY_DOWN")]
        KEY_DOWN = 0x88,
        [StringValue("KEY_STAR")]
        KEY_STAR = 0x8A,
        [StringValue("KEY_HASH")]
        KEY_HASH = 0x8B,
        [StringValue("KEY_INFO")]
        KEY_INFO = 0x8C,
        [StringValue("KEY_BYPASS")]
        KEY_BYPASS = 0x15,
        [StringValue("KEY_1")]
        KEY_1 = 0x01,
        [StringValue("KEY_2")]
        KEY_2 = 0x02
    };
}
