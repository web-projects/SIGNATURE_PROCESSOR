using System;
using System.Collections.Generic;

namespace DEVICE_SDK.Sdk
{
    // TODO: Refactor to use registry
    public class ConnectionConfig
    {
        public bool Valid { get; set; } = false;
        public bool Updated { get; set; }
        public DateTime LastLoaded { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<DeviceConfigData> ConnectionValues { get; } = new List<DeviceConfigData>();

        public List<object> ReadConfig()
        {
            Valid = true;
            Updated = false;
            LastLoaded = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            ConnectionValues.AddRange(new DeviceConfigData[] {
                new DeviceConfigData
                {
                    Manufacturer = "Mock",
                    ConnectInfos =
                    {
                        new DeviceConnectInfo
                        {
                            ConnectionType = DeviceConnectInfo.DeviceConnectionType.USB
                        }
                    },
                    DynamicLibraryName = "IPA5.DAL.Device.Mock.dll"
                },
                new DeviceConfigData
                {
                    Manufacturer = "IDTech_USDK",
                    ConnectInfos =
                    {
                        new DeviceConnectInfo
                        {
                            ConnectionType = DeviceConnectInfo.DeviceConnectionType.USB
                        },
                    },
                    DynamicLibraryName = "IPA5.DAL.Device.IDTech.IDTechUSDK.dll"
                },
                new DeviceConfigData
                {
                    Manufacturer = "Verifone",
                    ConnectInfos =
                    {
                        new DeviceConnectInfo
                        {
                            ConnectionType = DeviceConnectInfo.DeviceConnectionType.Comm,
                            Connection = "COM11"
                        }
                    },
                    DynamicLibraryName = "IPA5.DAL.Device.Verifone.VIPA.dll"
                }
            });

            return null;
        }
    }
}