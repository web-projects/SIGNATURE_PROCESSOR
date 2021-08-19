using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Common.Constants
{
    public static class Timeouts
    {
        //TODO: Those are defaultTimeouts. DAL should also be receiving those defaults from the Config.
        public static int DALCardCaptureTimeout = 90;
        public static int DALGetStatusTimeout = 10;
        public static int DALDeviceRecoveryTimeout = 30;

        public static int ServicerRequestDefaultTimeout = 300;
    }
}
