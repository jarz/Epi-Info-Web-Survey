using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Epi.Web.Common.Security;

namespace Epi.Web.WCF.SurveyService
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DataServiceAuthorizationAttribute : Attribute, IServiceBehavior
    {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers)
            {
                foreach (EndpointDispatcher endpoint in dispatcher.Endpoints)
                {
                    endpoint.DispatchRuntime.MessageInspectors.Add(new DataServiceAuthorizationInspector());
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        private sealed class DataServiceAuthorizationInspector : IDispatchMessageInspector
        {
            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
            {
                string presentedKey = null;
                int headerIndex = request.Headers.FindHeader(
                    DataServiceAuthentication.HeaderName, DataServiceAuthentication.HeaderNamespace);
                if (headerIndex >= 0)
                {
                    presentedKey = request.Headers.GetHeader<string>(headerIndex);
                }

                OperationContext operation = OperationContext.Current;
                Uri requestUri = operation == null || operation.Channel == null || operation.Channel.LocalAddress == null
                    ? null
                    : operation.Channel.LocalAddress.Uri;
                bool secureTransport = requestUri != null &&
                    string.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
                string configuredKey = ConfigurationManager.AppSettings[DataServiceAuthentication.SharedKeySetting];
                if (!DataServiceAuthentication.IsAuthorized(configuredKey, presentedKey, secureTransport))
                {
                    throw new FaultException("Access denied.");
                }

                return null;
            }

            public void BeforeSendReply(ref Message reply, object correlationState)
            {
            }
        }
    }
}
