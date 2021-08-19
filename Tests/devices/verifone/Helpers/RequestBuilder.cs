using XO.Common.DAL;
using XO.Requests;
using XO.Requests.DAL;
using System;
using System.Collections.Generic;

namespace Devices.Verifone.Tests.Helpers
{
    internal static class RequestBuilder
    {
        internal static LinkRequest LinkRequestGetDalStatus() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkGetDalStatusAction() },
                MessageID = RandomGenerator.BuildRandomString(8),
            };

        internal static LinkRequest LinkRequestSelectCreditOrDebit() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkGetCreditOrDebit() },
                MessageID = RandomGenerator.BuildRandomIntegerString(1),
            };

        internal static LinkRequest LinkGetPaymentRequest(int pinMaximumLength = 6) =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkPaymentRequestAction(pinMaximumLength) },
                MessageID = RandomGenerator.BuildRandomString(8),
            };

        internal static LinkRequest LinkGetPINRequest(int pinMaximumLength = 6) =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkGetPINAction(pinMaximumLength) },
                MessageID = RandomGenerator.BuildRandomIntegerString(4),
                LinkObjects = new LinkRequestIPA5Object()
                {
                    LinkActionResponseList = new List<XO.Responses.LinkActionResponse>()
                    { new XO.Responses.LinkActionResponse()
                    {
                        DALResponse = new XO.Responses.DAL.LinkDALResponse()
                        {
                            Devices = new List<XO.Responses.DAL.LinkDeviceResponse>()
                            {
                                new XO.Responses.DAL.LinkDeviceResponse()
                                {
                                    CardWorkflowControls = new LinkCardWorkflowControls()
                                }
                            }
                        }
                    }
                    }
                }
            };

        internal static LinkRequest LinkGetZipRequest() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkGetZipAction() },
                MessageID = RandomGenerator.BuildRandomIntegerString(5),
            };

        internal static LinkRequest LinkDeviceUI_SetIdle(bool resetDeviceEMVCollectionData = false) =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkDeviceUISetIdleAction(resetDeviceEMVCollectionData) },
                MessageID = RandomGenerator.BuildRandomIntegerString(5),
            };

        internal static LinkRequest LinkDeviceUI_Display(string message = "DISPLAY THIS MESSAGE") =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkDeviceUIDisplayAction(message) },
                MessageID = RandomGenerator.BuildRandomIntegerString(5),
            };

        internal static LinkRequest Link_StartAdaMode() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildStartAdaMode_Action() },
                MessageID = RandomGenerator.BuildRandomIntegerString(5),
            };

        internal static LinkRequest Link_EndAdaMode() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildEndAdaMode_Action() },
                MessageID = RandomGenerator.BuildRandomIntegerString(5),
            };

        internal static LinkRequest LinkCreditWorkflowRequest() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkCreditWorkflow() },
                MessageID = RandomGenerator.BuildRandomString(8),
            };

        internal static LinkRequest LinkDebitWorkflowRequest() =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest> { BuildLinkDebitWorkflow() },
                MessageID = RandomGenerator.BuildRandomString(8),
            };

        internal static LinkRequest LinkGetSignatureRequest(int pinMaximumLength = 6) =>
            new LinkRequest()
            {
                Actions = new List<LinkActionRequest>
                {
                    BuildLinkGetSignatureAction()
                },
                MessageID = RandomGenerator.BuildRandomIntegerString(4),
                LinkObjects = new LinkRequestIPA5Object()
                {
                    LinkActionResponseList = new List<XO.Responses.LinkActionResponse>()
                    {
                        new XO.Responses.LinkActionResponse()
                        {
                            DALResponse = new XO.Responses.DAL.LinkDALResponse()
                            {
                                Devices = new List<XO.Responses.DAL.LinkDeviceResponse>()
                                {
                                    new XO.Responses.DAL.LinkDeviceResponse()
                                    {
                                        CardWorkflowControls = new LinkCardWorkflowControls()
                                    }
                                }
                            }
                        }
                    }
                }
            };

        private static LinkActionRequest BuildLinkGetDalStatusAction()
        {
            var action = new LinkActionRequest
            {
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetStatus
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkGetCreditOrDebit()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetCreditOrDebit
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    RequestedAmount = 378,
                    CurrencyCode = "USD",
                    PaymentType = XO.Requests.Payment.LinkPaymentRequestType.Sale,
                    PartnerRegistryKeys = new List<string> { "CDTFsL8" },
                    RequestedTenderType = XO.Requests.Payment.LinkPaymentRequestedTenderType.Card,
                    PaymentAttributes = new XO.Requests.Payment.LinkPaymentAttributes
                    {
                        PartialPayment = true
                    },
                    CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                    {
                        DebitEnabled = true,
                        AVSEnabled = true,
                        AVSType = new List<string> { "ZIP" }
                    }
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkPaymentRequestAction(int pinMaximumLength)
        {
            var action = new LinkActionRequest
            {
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetPayment
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    RequestedAmount = 378,
                    CurrencyCode = "USD",
                    PaymentType = XO.Requests.Payment.LinkPaymentRequestType.Sale,
                    PartnerRegistryKeys = new List<string> { "CDTFsL8" },
                    RequestedTenderType = XO.Requests.Payment.LinkPaymentRequestedTenderType.Card,
                    PaymentAttributes = new XO.Requests.Payment.LinkPaymentAttributes
                    {
                        PartialPayment = true
                    },
                    CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                    {
                        DebitEnabled = false,
                        PinMaximumLength = pinMaximumLength
                    }
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkGetPINAction(int pinMaximumLength)
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetPIN
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    RequestedAmount = 378,
                    CurrencyCode = "USD",
                    PaymentType = XO.Requests.Payment.LinkPaymentRequestType.Sale,
                    PartnerRegistryKeys = new List<string> { "CDTFsL8" },
                    RequestedTenderType = XO.Requests.Payment.LinkPaymentRequestedTenderType.Card,
                    PaymentAttributes = new XO.Requests.Payment.LinkPaymentAttributes
                    {
                        PartialPayment = true
                    },
                    CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                    {
                        DebitEnabled = true,
                        PinMaximumLength = pinMaximumLength
                    }
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkGetZipAction()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetZIP
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    RequestedAmount = 378,
                    CurrencyCode = "USD",
                    PaymentType = XO.Requests.Payment.LinkPaymentRequestType.Sale,
                    PartnerRegistryKeys = new List<string> { "CDTFsL8" },
                    RequestedTenderType = XO.Requests.Payment.LinkPaymentRequestedTenderType.Card,
                    PaymentAttributes = new XO.Requests.Payment.LinkPaymentAttributes
                    {
                        PartialPayment = true
                    },
                    CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                    {
                        DebitEnabled = false,
                        AVSEnabled = true,
                        AVSType = new List<string> { "ZIP" }
                    }
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkCreditWorkflow()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetPayment
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    RequestedAmount = 378,
                    CurrencyCode = "USD",
                    PaymentType = XO.Requests.Payment.LinkPaymentRequestType.Sale,
                    PartnerRegistryKeys = new List<string> { "CDTFsL8" },
                    RequestedTenderType = XO.Requests.Payment.LinkPaymentRequestedTenderType.Card,
                    PaymentAttributes = new XO.Requests.Payment.LinkPaymentAttributes
                    {
                        PartialPayment = true
                    },
                    CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                    {
                        DebitEnabled = false
                    }
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkDebitWorkflow()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetPayment
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    RequestedAmount = 378,
                    CurrencyCode = "USD",
                    PaymentType = XO.Requests.Payment.LinkPaymentRequestType.Sale,
                    PartnerRegistryKeys = new List<string> { "CDTFsL8" },
                    RequestedTenderType = XO.Requests.Payment.LinkPaymentRequestedTenderType.Card,
                    PaymentAttributes = new XO.Requests.Payment.LinkPaymentAttributes
                    {
                        PartialPayment = true
                    },
                    CardWorkflowControls = new XO.Common.DAL.LinkCardWorkflowControls
                    {
                        DebitEnabled = true,
                        AVSEnabled = true,
                        AVSType = new List<string> { "ZIP" }
                    }
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkDeviceUISetIdleAction(bool resetDeviceEMVCollectionData)
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.DeviceUI,
                    DeviceUIRequest = new LinkDeviceUIRequest()
                    {
                        UIAction = LinkDeviceUIActionType.DisplayIdle,
                        ResetDeviceEMVCollectionData = resetDeviceEMVCollectionData
                    }
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    CardWorkflowControls = new LinkCardWorkflowControls()
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkDeviceUIDisplayAction(string message)
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.DeviceUI,
                    DeviceUIRequest = new LinkDeviceUIRequest()
                    {
                        UIAction = LinkDeviceUIActionType.Display,
                        DisplayText = new List<string>()
                        {
                            message
                        }
                    }
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    CardWorkflowControls = new LinkCardWorkflowControls()
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildStartAdaMode_Action()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.StartADAMode,
                    DeviceUIRequest = new LinkDeviceUIRequest
                    {
                        UIAction = LinkDeviceUIActionType.Display,
                        DisplayText = new List<string>
                        {
                            "Use Audio?"
                        }
                    }
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    CardWorkflowControls = new LinkCardWorkflowControls()
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildEndAdaMode_Action()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.EndADAMode
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest
                {
                    CardWorkflowControls = new LinkCardWorkflowControls()
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = null
                }
            };
            return action;
        }

        private static LinkActionRequest BuildLinkGetSignatureAction()
        {
            var action = new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.GetSignature
                },
                DALRequest = new LinkDALRequest
                {
                    LinkObjects = new LinkDALRequestIPA5Object()
                },
                PaymentRequest = new XO.Requests.Payment.LinkPaymentRequest()
            };
            return action;
        }

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
    }
}
