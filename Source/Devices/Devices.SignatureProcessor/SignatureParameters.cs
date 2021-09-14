namespace Devices.SignatureProcessor
{
    public static class SignatureParameters
    {
        // Per VIPA manual: coordinates are relative to screen coordinate space and device screen resolution dependent
        public const int M400BitmapWidth = 854;
        public const int M400BitmapHeight = 240;
        public static readonly string M400SignatureFilename = "Signature.png";
    }
}
