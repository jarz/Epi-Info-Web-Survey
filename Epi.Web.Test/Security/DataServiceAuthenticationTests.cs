using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;
using Epi.Web.Common.Security;
using Epi.Web.WCF.SurveyService;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class DataServiceAuthenticationTests
    {
        private static readonly string ValidKey = new string('a', 64);

        [Test]
        public void IsValidKey_accepts_64_character_hexadecimal_keys()
        {
            Assert.That(DataServiceAuthentication.IsValidKey(ValidKey), Is.True);
            Assert.That(DataServiceAuthentication.IsValidKey(new string('F', 64)), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-key")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaag")]
        public void IsValidKey_rejects_missing_or_malformed_keys(string key)
        {
            Assert.That(DataServiceAuthentication.IsValidKey(key), Is.False);
        }

        [Test]
        public void IsAuthorized_requires_https_and_matching_configured_keys()
        {
            Assert.That(DataServiceAuthentication.IsAuthorized(ValidKey, ValidKey, true), Is.True);
            Assert.That(DataServiceAuthentication.IsAuthorized(ValidKey, ValidKey, false), Is.False);
            Assert.That(DataServiceAuthentication.IsAuthorized(ValidKey, new string('b', 64), true), Is.False);
            Assert.That(DataServiceAuthentication.IsAuthorized(null, ValidKey, true), Is.False);
        }

        [Test]
        public void Client_behavior_rejects_an_unconfigured_key()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new DataServiceClientAuthorizationBehavior(""));
        }

        [Test]
        public void Client_behavior_requires_https_endpoints()
        {
            var behavior = new DataServiceClientAuthorizationBehavior(ValidKey);
            var endpoint = new ServiceEndpoint(
                ContractDescription.GetContract(typeof(IDataService)),
                new BasicHttpBinding(),
                new EndpointAddress("http://localhost/service"));
            var secureEndpoint = new ServiceEndpoint(
                ContractDescription.GetContract(typeof(IDataService)),
                new BasicHttpBinding(),
                new EndpointAddress("https://localhost/service"));

            Assert.Throws<InvalidOperationException>(() => behavior.Validate(endpoint));
            Assert.DoesNotThrow(() => behavior.Validate(secureEndpoint));
        }

        [Test]
        public void Every_service_version_requires_dispatch_authorization()
        {
            Type[] serviceTypes =
            {
                typeof(DataService),
                typeof(ManagerService),
                typeof(ManagerServiceV2),
                typeof(ManagerServiceV3),
                typeof(ManagerServiceV4)
            };

            foreach (Type serviceType in serviceTypes)
            {
                Assert.That(Attribute.IsDefined(serviceType, typeof(DataServiceAuthorizationAttribute), false), Is.True,
                    serviceType.FullName);
            }
        }
    }
}
