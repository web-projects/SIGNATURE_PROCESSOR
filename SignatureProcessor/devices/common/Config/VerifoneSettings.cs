﻿using System;
using System.Collections.Generic;

namespace Devices.Common.Config
{
    [Serializable]
    public class VerifoneSettings
    {
        public int SortOrder { get; set; } = -1;
        public List<string> SupportedDevices { get; internal set; } = new List<string>();
    }
}
