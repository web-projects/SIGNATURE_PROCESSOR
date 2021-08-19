using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using XO.Common.DAL;

namespace XO.Requests.Payment
{
    public class LinkPaymentRequest
    {
        public Responses.LinkErrorValue Validate(bool synchronousRequest = true)
        {
            if (MasterTCCustID != null && string.IsNullOrEmpty(MasterTCPassword))
            {
                return new Responses.LinkErrorValue     // TODO: update with correct values for missing required value
                {
                    Code = "MasterPasswordMissing",
                    Type = "MasterCustidRequiresPassword",
                    Description = "You are missing fields required for this request to be processed."
                };
            }

            // PreviousTCTransactionID validation has been left to servicer handler

            return null;
        }

        public long RequestedAmount { get; set; }
        public long RequestedAmountOther { get; set; }
        public string CurrencyCode { get; set; }
        public long? MasterTCCustID { get; set; }
        public string MasterTCPassword { get; set; }
        public string PreviousBillingID { get; set; }
        public string PreviousTCTransactionID { get; set; }
        public LinkPaymentRequestType? PaymentType { get; set; }
        public bool? CreateBillingID { get; set; }
        public bool? UpdateBillingIDTender { get; set; }
        //public LinkPayerRequest PayerValues { get; set; }
        //public LinkBankRequest BankAccountValues { get; set; }
        public bool? AVSVerify { get; set; }
        //public LinkReferenceInformation ReferenceInformation { get; set; }
        public List<string> PartnerRegistryKeys { get; set; }
        //public List<LinkCustomField> CustomFields { get; set; }
        public LinkPaymentRequestedTenderType RequestedTenderType { get; set; }
        public LinkPaymentAttributes PaymentAttributes { get; set; }
        //public LinkWorkflowControls WorkflowControls { get; set; }
        public LinkCardWorkflowControls CardWorkflowControls { get; set; }
        //public LinkIIASRequest IIASRequest { get; set; }
        public bool? Demo { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkPaymentRequestType
    {
        Sale,
        Preauth,
        Postauth,
        Void,
        ChargeBack,
        Cancel,
        Store,
        Unstore,
        Update,
        Verify,
        Reversal,
        Credit,
        Credit2,
        BalanceInquiry,
        Autovoid
    }

    //Payment tender types
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LinkPaymentRequestedTenderType
    {
        Unspecified,
        Card,
        Check
    }
}
