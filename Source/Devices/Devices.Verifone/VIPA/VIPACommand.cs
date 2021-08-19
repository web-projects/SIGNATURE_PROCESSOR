using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Verifone.VIPA
{
    public class VIPACommand
    {
        public byte nad { get; set; }
        public byte pcb { get; set; }
        public byte cla { get; set; }
        public byte ins { get; set; }
        public byte p1 { get; set; }
        public byte p2 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] data { get; set; }
        public bool includeLE { get; set; }
        public byte le { get; set; }
    }
}
