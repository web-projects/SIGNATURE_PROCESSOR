using Devices.Verifone.Helpers;
using Devices.Verifone.TLV;
using Devices.Verifone.Connection;
using System.Collections.Generic;
using static Devices.Verifone.VIPA.VIPA;
using SignatureProcessor.devices.Verifone.Helpers;

namespace Devices.Verifone.VIPA
{
    public interface IVIPA
    {
        bool Connect(string comPort, SerialConnection connection);

        void Dispose();
        
        void ResponseCodeHandler(List<TLV.TLV> tags, int responseCode, bool cancelled = false);

        bool DisplayMessage(VIPADisplayMessageValue displayMessageValue = VIPADisplayMessageValue.Idle, bool enableBacklight = false, string customMessage = "");

        (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceCommandReset();

        (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceExtendedReset();

        (DevicePTID devicePTID, int VipaResponse) DeviceReboot();

        (int VipaResult, int VipaResponse) GetActiveKeySlot();

        (SecurityConfigurationObject securityConfigurationObject, int VipaResponse) GetSecurityConfiguration(byte vssSlot, byte hostID);

        (KernelConfigurationObject kernelConfigurationObject, int VipaResponse) GetEMVKernelChecksum();

        int Configuration(string deviceModel);

        int ValidateConfiguration(string deviceModel);

        int FeatureEnablementToken();

        int LockDeviceConfiguration0();

        int LockDeviceConfiguration8();

        int UnlockDeviceConfiguration();

        (string HMAC, int VipaResponse) GenerateHMAC();

        int UpdateHMACKeys();

        (HTMLResponseObject htmlResponseObject, int VipaResponse) GetSignature();
    }
}