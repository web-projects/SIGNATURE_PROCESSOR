using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Text.RegularExpressions;

namespace Devices.Verifone.Connection
{
    public class DeviceDiscovery
    {
        public static readonly string VID = "11ca";
        const string UX300PID = "0201";
        const string P400PID = "0300";

        //TODO --> This needs to be investigeted with Verifone why devices give that PID.
        //JIRA ticket: https://jiraservicedesk.verifone.com/servicedesk/customer/portal/1/VS-23714
        const string GENPID = "aaaa";

        public List<USBDeviceInfo> deviceInfo { get; set; } = new List<USBDeviceInfo>();

        static private List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
            ManagementObjectCollection collection;

            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
            {
                collection = searcher.Get();

                foreach (var device in collection)
                {
                    var deviceID = ((string)device.GetPropertyValue("DeviceID") ?? "").ToLower(new CultureInfo("en-US", false));
                    if (string.IsNullOrWhiteSpace(deviceID))
                    {
                        continue;
                    }
                    //System.Diagnostics.Debug.WriteLine($"device: {deviceID}");
                    if (deviceID.Contains("usb\\") && deviceID.Contains($"vid_{VID}"))
                    {
                        if (device.GetPropertyValue("Service").ToString().IndexOf("VFIUNIUSB", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            devices.Add(new USBDeviceInfo(
                                (string)device.GetPropertyValue("DeviceID"),
                                (string)device.GetPropertyValue("PNPDeviceID"),
                                (string)device.GetPropertyValue("Description"),
                                (string)device.GetPropertyValue("Caption")
                            ));
                        }
                    }
                }
                collection.Dispose();
            }

            return devices;
        }

        public bool FindVerifoneDevices()
        {
            deviceInfo = GetUSBDevices();

            if (deviceInfo.Count >= 1)
            {
                foreach (var device in deviceInfo)
                {
                    //TODO: review if default of COM9 is any longer necessary after final Verifone fw/driver release.
                    Regex rg = new Regex(@"((COM[0-9]+))", RegexOptions.IgnoreCase);
                    MatchCollection matched = rg.Matches(device.Caption.ToUpper(new CultureInfo("en-US", false)));
                    device.ComPort = (matched[0]?.Value ?? "COM9");

                    string[] deviceCfg = Regex.Split(device.DeviceID, @"\\");
                    if (deviceCfg.Length == 3)
                    {
                        rg = new Regex(@"&PID_[0-9a-zA-Z\s]{0,4}", RegexOptions.IgnoreCase);
                        matched = rg.Matches(deviceCfg[1]);
                        if ((matched[0]?.Value.Substring(1).IndexOf(UX300PID, StringComparison.CurrentCultureIgnoreCase) > 0) ||
                            (matched[0]?.Value.Substring(1).IndexOf(P400PID, StringComparison.CurrentCultureIgnoreCase) > 0) ||
                            (matched[0]?.Value.Substring(1).IndexOf(GENPID, StringComparison.CurrentCultureIgnoreCase) > 0))
                        {
                            device.ProductID = matched[0]?.Value.Substring(1);
                            device.SerialNumber = deviceCfg[2];
                        }
                        else
                        {
                            return false;
                            throw new Exception($"Device doesn't have the expected identifier PID. The Current PID is '{matched[0]?.Value}'");
                        }
                    }
                }

                return true;
            }
            return false;
        }
    }
}
