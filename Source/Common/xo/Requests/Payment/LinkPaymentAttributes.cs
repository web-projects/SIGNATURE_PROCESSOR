namespace XO.Requests.Payment
{
    public class LinkPaymentAttributes
    {
        public Responses.LinkErrorValue Validate(bool synchronousRequest = true)
        {
            // No validation on these values
            return null;
        }

        public bool? PartialPayment { get; set; }
        public bool? SplitTender { get; set; }
        public bool? Installment { get; set; }
    }
}
