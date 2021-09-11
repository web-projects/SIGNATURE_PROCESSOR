using System.Collections.Generic;

namespace SignatureProcessorApp.devices.Verifone.Helpers
{
    public class HTMLResponseObject
    {
        public List<byte[]> SignatureData { get; set; }

        public HTMLResponseObject(List<byte[]> data) => (SignatureData) = (data);
    }
}
