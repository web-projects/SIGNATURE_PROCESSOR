namespace Devices.Common.Helpers.Templates
{
    public class TemplateHelper
    {
        public static bool IsTagDataUTF8(uint tagValue)
        {
            if (tagValue == E4Template.ApplicationLabel ||
                tagValue == E4Template.AppPreferredName ||
                tagValue == E4Template.CardholderName ||
                //tagValue ==E4Template.LanguagePreference) ||              //This tag is ASCII, but CORE doesn't accept it as ASCII for some reason?
                tagValue == E4Template.AuthorizationResponseCode ||
                tagValue == E4Template.InterfaceDeviceSerialNumber ||
                tagValue == E0Template.AuthorizationCode ||
                tagValue == EETemplate.TerminalId ||
                tagValue == E0Template.IssuerURL ||
                tagValue == E0Template.MerchantIdentifier ||
                tagValue == E0Template.MerchantNameLoc)
            {
                return true;
            }

            return false;
        }
    }
}
