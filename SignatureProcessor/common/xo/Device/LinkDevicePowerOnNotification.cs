namespace XO.Responses.Device
{
    public class LinkDevicePowerOnNotification
    {
        public int TransactionStatus { get; set; }
        public string TransactionStatusMessage { get; set; }
        public string TerminalID { get; set; }
    }
}
