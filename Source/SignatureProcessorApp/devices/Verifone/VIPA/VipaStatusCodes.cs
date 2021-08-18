using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Verifone.VIPA
{
    public enum VipaSW1SW2Codes
    {
        Success = 0x9000,
        CLessTransactionFail = 0x9f33,
        CLessCardInField = 0x9f36,
        CommandCancelled = 0x9f41,
        UserEntryCancelled = 0x9f43,
        Failure = 0xFFFF
    }
}
