using XO.Common.DAL;
using XO.Requests;
using XO.Requests.DAL;
using XO.Requests.Payment;
using XO.Responses;
using XO.Responses.DAL;
//using XO.Responses.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using XO.Requests;

namespace TestHelper
{
    public static class SampleBuilder
    {
        public static LinkRequest LinkRequestSale() => new LinkRequest()
        {
            //TCCustID = RandomGenerator.GetRandomValue(6),
            //TCPassword = RandomGenerator.BuildRandomString(7),
            //Actions = new List<LinkActionRequest> { BuildLinkSaleAction() },
            //MessageID = RandomGenerator.BuildRandomString(8),
            //IPALicenseKey = RandomGenerator.BuildRandomString(16)
        };

        //public static LinkRequest LinkRequestDALStatus() => new LinkRequest()
        //{
        //    TCCustID = RandomGenerator.GetRandomValue(6),
        //    TCPassword = RandomGenerator.BuildRandomString(7),
        //    Actions = new List<LinkActionRequest> { BuildLinkDALGetStatus() },
        //    MessageID = RandomGenerator.BuildRandomString(8),
        //    IPALicenseKey = RandomGenerator.BuildRandomString(16)
        //};

        //public static LinkRequest LinkRequestSession(LinkSessionActionType? linkSessionActionType = LinkSessionActionType.Initialize) => new LinkRequest()
        //{
        //    TCCustID = RandomGenerator.GetRandomValue(6),
        //    TCPassword = RandomGenerator.BuildRandomString(7),
        //    Actions = new List<LinkActionRequest> { BuildLinkSessionAction(linkSessionActionType) },
        //    MessageID = RandomGenerator.BuildRandomString(8),
        //    IPALicenseKey = RandomGenerator.BuildRandomString(16)
        //};

        public static LinkActionRequest BuildLinkSaleAction() => new LinkActionRequest
        {
            MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
            Action = LinkAction.Payment,
            DALRequest = PopulateMockDALIdentifier(null, false),
            PaymentRequest = new LinkPaymentRequest
            {
                RequestedAmount = RandomGenerator.GetRandomValue(4),
                CurrencyCode = "USD",
                PaymentType = LinkPaymentRequestType.Sale,
                //WorkflowControls = new LinkWorkflowControls() { CardEnabled = true },
                PartnerRegistryKeys = new List<string>() { RandomGenerator.BuildRandomString(7) },
                RequestedTenderType = LinkPaymentRequestedTenderType.Card,
                CardWorkflowControls = new LinkCardWorkflowControls()
            }
        };

        public static LinkActionRequest BuildLinkDALGetStatus() => new LinkActionRequest()
        {
            MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
            Action = LinkAction.DALAction,
            DALRequest = PopulateMockDALIdentifier(null, false),
            DALActionRequest = new LinkDALActionRequest()
            {
                DALAction = LinkDALActionType.GetStatus
            }
        };

        //public static LinkRequestIPA5Object LinkObjectsForEvents() => new LinkRequestIPA5Object()
        //{
        //    LinkActionResponseList = new List<LinkActionResponse>()
        //    {
        //        new LinkActionResponse()
        //        {
        //            MessageID = RandomGenerator.BuildRandomString(5),
        //            DALResponse = new LinkDALResponse(),
        //            PaymentResponse = new LinkPaymentResponse()
        //            {
        //                TCLinkResponse = new List<LinkNameValueResponse>()
        //            }
        //        }
        //    }
        //};

        public static LinkRequestIPA5Object LinkRequestWithDeviceResponse(string manufacturer) => new LinkRequestIPA5Object()
        {
            LinkActionResponseList = new List<LinkActionResponse>()
            {
                new LinkActionResponse()
                {
                    DALResponse = new LinkDALResponse()
                    {
                        Devices = new List<XO.Responses.DAL.LinkDeviceResponse>()
                        {
                            new XO.Responses.DAL.LinkDeviceResponse()
                            {
                                Manufacturer = manufacturer
                            }
                        }
                    }
                }
            }
        };

        //public static LinkRequest SetDALStatusOnline(LinkRequest request)
        //{
        //    var linkActionRequest = request.Actions.First();

        //    request.LinkObjects = LinkRequestWithDeviceResponse("MOCK");

        //    if (string.IsNullOrWhiteSpace(linkActionRequest.RequestID))
        //    {
        //        linkActionRequest.RequestID = Guid.NewGuid().ToString();
        //    }

        //    var linkActionResponse = new LinkActionResponse
        //    {
        //        MessageID = linkActionRequest.MessageID,
        //        RequestID = linkActionRequest.RequestID,
        //        DALResponse = new LinkDALResponse
        //        {
        //            DALIdentifier = MockDALIdentifier(null)
        //        }
        //    };

        //    if (request.LinkObjects == null)
        //    {
        //        request.LinkObjects = new LinkRequestIPA5Object()
        //        {
        //            RequestID = new Guid(linkActionRequest.RequestID),
        //            LinkActionResponseList = new List<LinkActionResponse>(),
        //            IdleRequest = false
        //        };
        //    }

        //    linkActionRequest.LinkObjects = new LinkActionRequestIPA5Object
        //    {
        //        RequestID = request.LinkObjects.RequestID,
        //        ActionResponse = linkActionResponse
        //    };

        //    return request;
        //}

        //public static LinkNameValueResponse TCLinkResponse(string name, string value) => new LinkNameValueResponse()
        //{
        //    Name = name,
        //    Value = value
        //};

        //private static LinkActionRequest BuildLinkSessionAction(LinkSessionActionType? linkSessionActionType) => new LinkActionRequest()
        //{
        //    MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
        //    Action = LinkAction.Session,
        //    DALRequest = PopulateMockDALIdentifier(null, false),
        //    SessionRequest = new LinkSessionRequest
        //    {
        //        SessionAction = linkSessionActionType,
        //        IdleActions = new List<LinkActionRequest>()
        //        {
        //            new LinkActionRequest()
        //            {
        //                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
        //                Action = LinkAction.DALAction,
        //                DALActionRequest = new LinkDALActionRequest()
        //                {
        //                    DALAction = LinkDALActionType.DeviceUI,
        //                    DeviceUIRequest = new LinkDeviceUIRequest()
        //                    {
        //                        UIAction = LinkDeviceUIActionType.KeyRequest,
        //                        //AutoConfirmKey = true,
        //                        //ReportCardPresented = true,
        //                        //MinLength = 1,
        //                        //MaxLength = 1
        //                    }
        //                }
        //            }
        //        }
        //    }
        //};

        //public static LinkDALResponse MockDALResponse() => new LinkDALResponse
        //{
        //    DALIdentifier = MockDALIdentifier(null)
        //};

        //public static LinkDALActionResponse MockDALActionResponse(bool mode) => new LinkDALActionResponse
        //{
        //    Status = "ADAMODE",
        //    Value = mode ? "ON" : "OFF"
        //};

        //public static LinkDALActionResponse MockDALUIActionResponse() => new LinkDALActionResponse
        //{
        //    Status = "DeviceSetToIdle"
        //};

        private static LinkDALRequest PopulateMockDALIdentifier(LinkDALRequest linkDALRequest, bool addLookupPreference = true)
        {
            if (linkDALRequest == null)
            {
                linkDALRequest = new LinkDALRequest();
            }

            if (linkDALRequest.DALIdentifier == null)
            {
                linkDALRequest.DALIdentifier = MockDALIdentifier(linkDALRequest.DALIdentifier);
            }

            if (addLookupPreference && (linkDALRequest.DALIdentifier.LookupPreference == null))
            {
                linkDALRequest.DALIdentifier.LookupPreference = LinkDALLookupPreference.WorkstationName;
            }

            return linkDALRequest;
        }

        private static LinkDALIdentifier MockDALIdentifier(LinkDALIdentifier dalIdentifier)
        {
            if (dalIdentifier != null)
            {
                return dalIdentifier;
            }

            var rnd = new Random();
            dalIdentifier = new LinkDALIdentifier
            {
                DnsName = "Host" + rnd.Next(0, 999).ToString("000", null),
                IPv4 = rnd.Next(193, 254).ToString("000", null) + "." + rnd.Next(193, 254).ToString("000", null) + "." + rnd.Next(193, 254).ToString("000", null) + "." + rnd.Next(193, 254).ToString("000", null),
                Username = "User" + RandomGenerator.BuildRandomString(6)
            };

            return dalIdentifier;
        }

        public static LinkRequest LinkRequestCancelPayment() => new LinkRequest()
        {
            Actions = new List<LinkActionRequest> { BuildLinkDALCancelPayment() },
            MessageID = RandomGenerator.BuildRandomString(8)
        };

        public static LinkActionRequest BuildLinkDALCancelPayment() => new LinkActionRequest()
        {
            MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
            Action = LinkAction.DALAction,
            DALRequest = PopulateMockDALIdentifier(null, false),
            DALActionRequest = new LinkDALActionRequest()
            {
                DALAction = LinkDALActionType.CancelPayment
            }
        };

        public static LinkRequest LinkRequestEndPreSwipeMode() => new LinkRequest()
        {
            Actions = new List<LinkActionRequest> { BuildLinkDALEndPreSwipeMode() },
            MessageID = RandomGenerator.BuildRandomString(8)
        };

        public static LinkActionRequest BuildLinkDALEndPreSwipeMode() => new LinkActionRequest()
        {
            MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
            Action = LinkAction.DALAction,
            DALRequest = PopulateMockDALIdentifier(null, false),
            DALActionRequest = new LinkDALActionRequest()
            {
                DALAction = LinkDALActionType.EndPreSwipeMode
            }
        };

        public static LinkRequest LinkRequestVoid() => new LinkRequest()
        {
            //TCCustID = RandomGenerator.GetRandomValue(6),
            //TCPassword = RandomGenerator.BuildRandomString(7),
            //Actions = new List<LinkActionRequest> { BuildLinkVoidAction() },
            //MessageID = RandomGenerator.BuildRandomString(8),
            //IPALicenseKey = RandomGenerator.BuildRandomString(16)
        };

        public static LinkActionRequest BuildLinkVoidAction() => new LinkActionRequest
        {
            MessageID = RandomGenerator.BuildRandomString(RandomGenerator.Random.Next(5, 16)),
            Action = LinkAction.Payment,
            PaymentRequest = new LinkPaymentRequest()
            {
                RequestedAmount = 0,
                PaymentType = LinkPaymentRequestType.Void,
                PartnerRegistryKeys = new List<string>() { RandomGenerator.BuildRandomString(7) },
                PreviousTCTransactionID = $"{RandomGenerator.BuildRandomIntegerString(3)}-{RandomGenerator.BuildRandomIntegerString(10)}"
            }
        };

        public static LinkRequest LinkRequestACHSale()
        {
            LinkRequest request = LinkRequestSale();
            //request.Actions[0].PaymentRequest.BankAccountValues = new LinkBankRequest()
            //{
            //    AccountNumber = "55544433221",
            //    RoutingNumber = "789456124",
            //    BankAccountType = LinkBankAccountType.Personal
            //};
            request.Actions[0].PaymentRequest.RequestedTenderType = LinkPaymentRequestedTenderType.Check;
            return request;
        }

        //public static XO.ProtoBuf.CommunicationHeader ServicerHeader() =>
        //    new XO.ProtoBuf.CommunicationHeader()
        //    {
        //        Flags = new XO.ProtoBuf.CommFlags(),
        //        CommIdentifiers = { new XO.ProtoBuf.CommIdentifier()
        //        {
        //            DnsName = "DNSName",
        //            Service = XO.ProtoBuf.ServiceType.Receiver
        //        } }
        //    };
    }
}
