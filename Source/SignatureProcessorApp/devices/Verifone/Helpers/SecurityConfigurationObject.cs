using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Verifone.Helpers
{
    public class SecurityConfigurationObject
    {
        public byte VSSPrimarySlot { get; } = 0x02;
        public int PrimarySlot { get; } = 0x06;
        public int SecondarySlot { get; } = 0x07;
        public byte ADEProductionSlot { get; } = 0x00;
        public byte ADETestSlot { get; } = 0x08;
        public string OnlinePinKSN { get; set; }
        public string KeySlotNumber { get; set; }
        public string SRedCardKSN { get; set; }
        public string InitVector { get; set; }
        public string EncryptedKeyCheck { get; set; }
        public string GeneratedHMAC { get; set; }
    }
}
