using System;
using System.Collections.Generic;
using System.Threading;
using XO.Requests;
using XO.Responses;

namespace Devices.Common.Interfaces
{
    //public delegate void PublishEvent(EventTypeType eventType, EventCodeType eventCode,
    //        List<object> devices, object request, string message);

    public interface ICardDevice : ICloneable
    {
        //event PublishEvent PublishEvent;
        event DeviceEventHandler DeviceEventOccured;

        string Name { get; }

        string ManufacturerConfigID { get; }

        int SortOrder { get; set; }

        DeviceInformation DeviceInformation { get; }

        bool IsConnected(LinkRequest request);

        List<DeviceInformation> DiscoverDevices();

        List<LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool dalActive);

        void DeviceSetIdle();

		bool DeviceRecovery();

        void Disconnect();

        List<LinkRequest> GetDeviceResponse(LinkRequest deviceInfo);

        // ------------------------------------------------------------------------
        // Methods that are mapped for usage in their respective sub-workflows.
        // ------------------------------------------------------------------------
        LinkRequest GetStatus(LinkRequest linkRequest);
        LinkRequest GetActiveKeySlot(LinkRequest linkRequest);
        LinkRequest GetEMVKernelChecksum(LinkRequest linkRequest);
        LinkRequest GetSecurityConfiguration(LinkRequest linkRequest);
        LinkRequest AbortCommand(LinkRequest linkRequest);
        LinkRequest ResetDevice(LinkRequest linkRequest);
        LinkRequest RebootDevice(LinkRequest linkRequest);
        LinkRequest Configuration(LinkRequest linkRequest);
        LinkRequest FeatureEnablementToken(LinkRequest linkRequest);
        LinkRequest LockDeviceConfiguration0(LinkRequest linkRequest);
        LinkRequest LockDeviceConfiguration8(LinkRequest linkRequest);
        LinkRequest UnlockDeviceConfiguration(LinkRequest linkRequest);
        LinkRequest UpdateHMACKeys(LinkRequest linkRequest);
        LinkRequest GenerateHMAC(LinkRequest linkRequest);
    }
}
