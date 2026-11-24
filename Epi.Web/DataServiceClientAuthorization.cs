using Epi.Web.Common.Security;

namespace Epi.Web.MVC.DataServiceClient
{
    public sealed class AuthenticatedDataServiceClient : DataServiceClient
    {
        public AuthenticatedDataServiceClient(string endpointConfigurationName, string sharedKey)
            : base(endpointConfigurationName)
        {
            Endpoint.EndpointBehaviors.Add(new DataServiceClientAuthorizationBehavior(sharedKey));
        }
    }
}
