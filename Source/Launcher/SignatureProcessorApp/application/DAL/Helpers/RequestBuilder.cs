﻿using System;
using System.Collections.Generic;
using System.Text;
using XO.Common.DAL;
using XO.Private;
using XO.Requests;
using XO.Requests.DAL;

namespace SignatureProcessorApp.application.DAL.Helpers
{
    public static class RequestBuilder
    {
        internal static LinkRequest LinkAbortCommandRequest() =>
             new LinkRequest()
             {
                 MessageID = RandomGenerator.BuildRandomIntegerString(4),
                 Actions = new List<LinkActionRequest>
                 {
                    BuildLinkAbortCommandAction()
                 }
             };

        internal static LinkRequest LinkGetSignatureRequest(int pinMaximumLength = 6) =>
            new LinkRequest()
            {
                MessageID = RandomGenerator.BuildRandomIntegerString(4),
                Actions = new List<LinkActionRequest>
                {
                    BuildLinkGetSignatureAction()
                },
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

        
        private static LinkActionRequest BuildLinkAbortCommandAction()
        {
            return new LinkActionRequest
            {
                MessageID = RandomGenerator.BuildRandomString(RandomGenerator.rnd().Next(5, 16)),
                Action = LinkAction.DALAction,
                DALActionRequest = new LinkDALActionRequest
                {
                    DALAction = LinkDALActionType.SendReset
                },
                DeviceRequest = new LinkDeviceRequest()
                {
                    DeviceIdentifier = new XO.Device.LinkDeviceIdentifier()
                    {
                        SerialNumber = "DEADBEEF"
                    }
                }
            };
        }

        private static LinkActionRequest BuildLinkGetSignatureAction()
        {
            return new LinkActionRequest
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
        }
    }
}
