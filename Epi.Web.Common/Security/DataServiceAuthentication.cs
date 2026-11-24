using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using WcfMessage = System.ServiceModel.Channels.Message;

namespace Epi.Web.Common.Security
{
    public static class DataServiceAuthentication
    {
        public const string SharedKeySetting = "DataServiceSharedKey";
        public const string HeaderName = "DataServiceSharedKey";
        public const string HeaderNamespace = "urn:epi-info:data-service-auth";
        private const int SharedKeyLength = 64;

        public static bool IsValidKey(string key)
        {
            if (key == null || key.Length != SharedKeyLength)
            {
                return false;
            }

            for (int i = 0; i < key.Length; i++)
            {
                char value = key[i];
                if (!((value >= '0' && value <= '9') ||
                      (value >= 'a' && value <= 'f') ||
                      (value >= 'A' && value <= 'F')))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsAuthorized(string configuredKey, string presentedKey, bool secureTransport)
        {
            if (!secureTransport || !IsValidKey(configuredKey) || !IsValidKey(presentedKey))
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < SharedKeyLength; i++)
            {
                difference |= configuredKey[i] ^ presentedKey[i];
            }

            return difference == 0;
        }
    }

    public sealed class DataServiceClientAuthorizationBehavior : IEndpointBehavior
    {
        private readonly string sharedKey;

        public DataServiceClientAuthorizationBehavior(string sharedKey)
        {
            if (!DataServiceAuthentication.IsValidKey(sharedKey))
            {
                throw new ConfigurationErrorsException(
                    "AppSetting '" + DataServiceAuthentication.SharedKeySetting +
                    "' must contain a 64-character hexadecimal key.");
            }

            this.sharedKey = sharedKey;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new DataServiceClientAuthorizationInspector(sharedKey));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            if (endpoint.Address == null || endpoint.Address.Uri == null ||
                !string.Equals(endpoint.Address.Uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Epi Info Web Survey service endpoint must use HTTPS.");
            }
        }

        private sealed class DataServiceClientAuthorizationInspector : IClientMessageInspector
        {
            private readonly string sharedKey;

            public DataServiceClientAuthorizationInspector(string sharedKey)
            {
                this.sharedKey = sharedKey;
            }

            public object BeforeSendRequest(ref WcfMessage request, IClientChannel channel)
            {
                request.Headers.Add(MessageHeader.CreateHeader(
                    DataServiceAuthentication.HeaderName,
                    DataServiceAuthentication.HeaderNamespace,
                    sharedKey));
                return null;
            }

            public void AfterReceiveReply(ref WcfMessage reply, object correlationState)
            {
            }
        }
    }
}
