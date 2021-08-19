namespace SignatureProcessorApp.devices.common
{
    public class SupportedTransactions
    {
        public bool EnableContactlessMSR { get; set; } = true;
        public bool ContactlessMSRConfigIsValid { get; set; } = false;
        public bool EnableContactEMV { get; set; } = false;
        public bool ContactEMVConfigIsValid { get; set; } = false;
        public bool EnableContactlessEMV { get; set; } = false;
        public bool ContactlessEMVConfigIsValid { get; set; } = false;
        public bool EMVKernelValidated { get; set; } = false;
    }
}
