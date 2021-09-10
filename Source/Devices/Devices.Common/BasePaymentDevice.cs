using System;
using System.Collections.Generic;
using System.Threading;
using XO.Responses.DAL;

namespace Devices.Common
{
    public abstract class BasePaymentDevice : IPaymentDevice, IComparable
    {
        public event PublishEvent PublishEvent;
        public event PublishEvent PublishMonitor;
        public event DeviceLogHandler DeviceLogOccured;
        public event DeviceEventHandler DeviceEventOccured;

        //protected CommunicationHeader commHeader;

        public DeviceInformation DeviceInformation { get; set; }

        public abstract string Name { get; }

        public abstract string ManufacturerConfigID { get; }

        public virtual List<string> TransactionConfigurations { get; protected set; }
        public virtual int SortOrder { get; set; } = -1;

        //public virtual void SetRequestHeader(CommunicationHeader header) => commHeader = header;

        public virtual List<LinkDeviceResponse> GetDeviceResponse(LinkDeviceResponse deviceInfo)
        {
            List<LinkDeviceResponse> deviceResponse = new List<LinkDeviceResponse>
            {
                new LinkDeviceResponse
                {
                    Manufacturer = ManufacturerConfigID,
                    Model = deviceInfo?.Model,
                    SerialNumber = deviceInfo?.SerialNumber,
                    TerminalId = deviceInfo?.TerminalId,
                    FirmwareVersion = deviceInfo?.FirmwareVersion
                }
            };

            return deviceResponse;
        }

        public abstract object Clone();

        public virtual void Dispose()
        {
        }

        public abstract bool DeviceRecovery();

        public abstract bool DeviceRecoveryWithMessagePreservation();

        public abstract void DeviceSetIdle();

        public abstract void Disconnect();

        public abstract List<DeviceInformation> DiscoverDevices();

        public abstract int GetDeviceHealthStatus();

        //public abstract int SetDeviceConfigFromCDB(MultiCDBDataResponse cdbDataResponse);

        public abstract XO.Requests.LinkRequest GetStatus(XO.Requests.LinkRequest request, CancellationToken cancellationToken);

        public abstract bool IsConnected(XO.Requests.LinkRequest request);

        public abstract List<XO.Responses.LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool dalActive);

        //public abstract void SetDeviceSectionConfig(DeviceSection config);

        public virtual int CompareTo(object obj)
        {
            if (obj is IPaymentDevice device)
            {
                if (SortOrder > device.SortOrder)
                    return 1;

                if (SortOrder < device.SortOrder)
                    return -1;
            }
            return 0;
        }

        //protected void RaisePublishMonitor(EventTypeType type, EventCodeType code, LinkDeviceResponse linkDeviceResponse, IPA5.XO.Requests.LinkRequest request, string message)
        //{
        //    PublishMonitor?.Invoke(type, code, GetDeviceResponse(linkDeviceResponse), CommObject(request), message);
        //}

        //protected void RaisePublishEvent(EventTypeType type, EventCodeType code, LinkDeviceResponse linkDeviceResponse, IPA5.XO.Requests.LinkRequest request, string message)
        //{
        //    PublishEvent?.Invoke(type, code, GetDeviceResponse(linkDeviceResponse), CommObject(request), message);
        //}

        //protected void RaiseDeviceLog(LogLevel level, string message)
        //{
        //    DeviceLogOccured?.Invoke(level, message);
        //}

        //protected void RaiseDeviceEvent(DeviceEvent deviceEvent, DeviceInformation information)
        //{
        //    DeviceEventOccured?.Invoke(deviceEvent, information);
        //}

        //protected CommunicationObject CommObject(IPA5.XO.Requests.LinkRequest request) => new CommunicationObject(commHeader, request);

        //protected LinkDALRequestIPA5Object ErrorResponse(EventCodeType codeType, EventTypeType errorCode, string descStr)
        //{
        //    return new LinkDALRequestIPA5Object
        //    {
        //        DALResponseData = new LinkDALActionResponse
        //        {
        //            Errors = new List<XO.Responses.LinkErrorValue>
        //            {
        //                new XO.Responses.LinkErrorValue
        //                {
        //                    Code = Enum.GetName(typeof(EventCodeType), codeType),
        //                    Type = errorCode.GetStringValue(),
        //                    Description = descStr ?? string.Empty
        //                }
        //            }
        //        }
        //    };
        //}

        //protected LinkDALRequestIPA5Object ErrorResponse(EventCodeType codeType, int errorCode, string descStr)
        //{
        //    return new LinkDALRequestIPA5Object
        //    {
        //        DALResponseData = new LinkDALActionResponse
        //        {
        //            Errors = new List<XO.Responses.LinkErrorValue>
        //            {
        //                new XO.Responses.LinkErrorValue
        //                {
        //                    Code = Enum.GetName(typeof(EventCodeType), codeType),
        //                    Type = errorCode.ToString(),
        //                    Description = descStr ?? string.Empty
        //                }
        //            }
        //        }
        //    };
        //}

    }
}
