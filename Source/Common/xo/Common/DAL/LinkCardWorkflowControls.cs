using System.Collections.Generic;

namespace XO.Common.DAL
{
    public class LinkCardWorkflowControls
    {
        public bool? DebitEnabled { get; set; }
        public bool? EMVEnabled { get; set; }
        public bool? ContactlessEnabled { get; set; }
        public bool? ContactlessEMVEnabled { get; set; }
        public bool? CVVEnabled { get; set; }
        public bool? VerifyAmountEnabled { get; set; }
        public bool? CardExpEnabled { get; set; }
        public bool? AVSEnabled { get; set; }
        public List<string> AVSType { get; set; }
        public int? PinMaximumLength { get; set; }
    }
}
