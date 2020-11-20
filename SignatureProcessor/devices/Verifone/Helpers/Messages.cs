using Devices.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Verifone.Helpers
{
    class Messages
    {
        public enum ConsoleMessages
        {
            [StringValue("VIPA: GET DEVICE INFO")]
            GetDeviceInfo,
            [StringValue("VIPA: GET DEVICE HEALTH")]
            GetDeviceHealth,
            [StringValue("VIPA: LOCK DEVICE UPDATE")]
            LockDeviceUpdate,
            [StringValue("VIPA: UNLOCK DEVICE UPDATE")]
            UnlockDeviceUpdate,
            [StringValue("VIPA: UPDATE DEVICE CONFIGURATION")]
            UpdateDeviceUpdate,
            [StringValue("VIPA: UPDATE HMAC KEYS")]
            UpdateHMACKeys,
            [StringValue("VIPA: GENERATE HMAC")]
            GenerateHMAC,
            [StringValue("VIPA: RESET")]
            DeviceReset,
            [StringValue("VIPA: EXTENDED RESET")]
            DeviceExtendedReset,
            [StringValue("VIPA: ABORT COMMAND")]
            AbortCommand,
            [StringValue("VIPA: REBOOT DEVICE")]
            RebootDevice,
            [StringValue("VIPA: GET CARD STATUS")]
            GetCardStatus,
            [StringValue("VIPA: GET CARD INFO")]
            GetCardInfo,
            [StringValue("VIPA: ENTER CARD TYPE")]
            GetCardType,
            [StringValue("VIPA: ENTER ZIP")]
            GetZipCode,
            [StringValue("VIPA: ENTER PIN")]
            GetPIN,
            [StringValue("VIPA: ENTER ADA MODE")]
            StartADA,
            [StringValue("VIPA: CLESS READER CLOSED")]
            DeviceCLessReaderClosed,
            [StringValue("VIPA: GET SECURITY CONFIGURATION")]
            GetSecurityConfiguration
        }
    }
}
