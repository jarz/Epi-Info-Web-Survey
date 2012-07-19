﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Configuration;
using Epi.Web.Common.Exception;

namespace Epi.Web.SurveyManager.Client
{
    public class ServiceClient
    {
        public static SurveyManagerService.ManagerServiceClient GetClient(string pEndPointAddress, bool pIsAuthenticated)
        {
            SurveyManagerService.ManagerServiceClient result = null;
            try
            {


                if (pIsAuthenticated) // Windows Authentication
                {
                    
                    System.ServiceModel.BasicHttpBinding binding = new System.ServiceModel.BasicHttpBinding();
                    binding.Name = "BasicHttpBinding";
                    binding.CloseTimeout = new TimeSpan(0, 1, 0);
                    binding.OpenTimeout = new TimeSpan(0, 1, 0);
                    binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                    binding.SendTimeout = new TimeSpan(0, 1, 0);
                    binding.AllowCookies = false;
                    binding.BypassProxyOnLocal = false;
                    binding.HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;
                    binding.MaxBufferPoolSize = 524288;
                    binding.MaxReceivedMessageSize = 65536;
                    binding.MessageEncoding = System.ServiceModel.WSMessageEncoding.Text;
                    binding.TextEncoding = System.Text.Encoding.UTF8;
                    binding.TransferMode = System.ServiceModel.TransferMode.Buffered;
                    binding.UseDefaultWebProxy = true;
                    binding.ReaderQuotas.MaxDepth = 32;
                    binding.ReaderQuotas.MaxStringContentLength = 8192;
                    binding.ReaderQuotas.MaxArrayLength = 16384;
                    binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    binding.ReaderQuotas.MaxNameTableCharCount = 16384;

                    binding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.TransportCredentialOnly;
                    binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Windows;
                    binding.Security.Transport.ProxyCredentialType = System.ServiceModel.HttpProxyCredentialType.None;
                    binding.Security.Transport.Realm = string.Empty;

                    binding.Security.Message.ClientCredentialType = System.ServiceModel.BasicHttpMessageCredentialType.UserName;

                    System.ServiceModel.EndpointAddress endpoint = new System.ServiceModel.EndpointAddress(pEndPointAddress);
                    
                    result = new SurveyManagerService.ManagerServiceClient(binding, endpoint);
                    result.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                    result.ChannelFactory.Credentials.Windows.ClientCredential = System.Net.CredentialCache.DefaultNetworkCredentials;
                }
                else
                {
                    
                    System.ServiceModel.WSHttpBinding binding = new System.ServiceModel.WSHttpBinding();
                    binding.Name = "WSHttpBinding";
                    binding.CloseTimeout = new TimeSpan(0, 1, 0);
                    binding.OpenTimeout = new TimeSpan(0, 1, 0);
                    binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                    binding.SendTimeout = new TimeSpan(0, 1, 0);
                    binding.BypassProxyOnLocal = false;
                    binding.TransactionFlow = false;
                    binding.HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard;
                    binding.MaxBufferPoolSize = 524288;
                    binding.MaxReceivedMessageSize = 65536;
                    binding.MessageEncoding = System.ServiceModel.WSMessageEncoding.Text;
                    binding.TextEncoding = System.Text.Encoding.UTF8;
                    binding.UseDefaultWebProxy = true;
                    binding.AllowCookies = false;

                    binding.ReaderQuotas.MaxDepth = 32;
                    binding.ReaderQuotas.MaxStringContentLength = 8192;
                    binding.ReaderQuotas.MaxArrayLength = 16384;
                    binding.ReaderQuotas.MaxBytesPerRead = 4096;
                    binding.ReaderQuotas.MaxNameTableCharCount = 16384;

                    binding.ReliableSession.Ordered = true;
                    binding.ReliableSession.InactivityTimeout = new TimeSpan(0, 10, 0);
                    binding.ReliableSession.Enabled = false;

                    binding.Security.Mode = System.ServiceModel.SecurityMode.Message;
                    binding.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Windows;
                    binding.Security.Transport.ProxyCredentialType = System.ServiceModel.HttpProxyCredentialType.None;
                    binding.Security.Transport.Realm = string.Empty;
                    binding.Security.Message.ClientCredentialType = System.ServiceModel.MessageCredentialType.Windows;
                    binding.Security.Message.NegotiateServiceCredential = true;
                    
                    System.ServiceModel.EndpointAddress endpoint = new System.ServiceModel.EndpointAddress(pEndPointAddress);

                    result = new SurveyManagerService.ManagerServiceClient(binding, endpoint);
                }

            }
            catch (FaultException<CustomFaultException> cfe)
            {
                throw cfe;
            }
            catch (FaultException fe)
            {
                throw fe;
            }
            catch (SecurityNegotiationException sne)
            {
                throw sne;
            }
            catch (CommunicationException ce)
            {
                throw ce;
            }
            catch (TimeoutException te)
            {
                throw te;
            }
            catch (Exception ex)
            {
                throw ex;
            }
                return result;
        }

        public static SurveyManagerService.ManagerServiceClient GetClient()
        {
            string pEndPointAddress = ConfigurationManager.AppSettings["EndPointAddress"];
            bool pIsAuthenticated = false;

            string s = ConfigurationManager.AppSettings["Authentication_Use_Windows"];
            if (!String.IsNullOrEmpty(s))
            {
                if (s.ToUpper() == "TRUE")
                {
                    pIsAuthenticated = true;
                }
            }

            return GetClient(pEndPointAddress, pIsAuthenticated); 
        }
    }
}