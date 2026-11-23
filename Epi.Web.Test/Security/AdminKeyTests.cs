using System;
using System.Collections.Specialized;
using System.Configuration;
using Epi.Web.Common.Security;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class AdminKeyTests
    {
        private static readonly Guid ConfiguredKey = new Guid("5d1f0c7e-2b8a-4c3d-9e6f-a1b2c3d4e5f6");

        private static NameValueCollection WithAdminKey(string value)
        {
            NameValueCollection settings = KeyMaterialTests.Settings(KeyMaterialTests.TestKeys);
            settings[AdminKey.Setting] = value;
            return settings;
        }

        [Test]
        public void Read_returns_null_when_no_admin_key_is_configured()
        {
            Assert.That(AdminKey.Read(WithAdminKey(""), KeyMaterialTests.TestKeys), Is.Null);
        }

        [Test]
        public void Read_and_Matches_accept_the_configured_key_in_any_case()
        {
            string encrypted = Cryptography.Encrypt(ConfiguredKey.ToString("D"), KeyMaterialTests.TestKeys);

            Guid? configured = AdminKey.Read(WithAdminKey(encrypted), KeyMaterialTests.TestKeys);

            Assert.That(configured, Is.EqualTo(ConfiguredKey));
            Assert.That(AdminKey.Matches(ConfiguredKey.ToString("D").ToUpperInvariant(), configured), Is.True);
        }

        [TestCase("9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        [TestCase("not-a-guid")]
        [TestCase("")]
        [TestCase(null)]
        public void Matches_rejects_any_other_key(string supplied)
        {
            Assert.That(AdminKey.Matches(supplied, ConfiguredKey), Is.False);
        }

        [Test]
        public void Matches_rejects_everything_when_no_key_is_configured()
        {
            Assert.That(AdminKey.Matches(ConfiguredKey.ToString(), null), Is.False);
        }

        [TestCase("not base64 at all")]
        [TestCase("encrypted with other keys")]
        [TestCase("not-a-guid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public void Read_rejects_values_that_do_not_decrypt_to_a_usable_guid(string scenario)
        {
            string value;
            switch (scenario)
            {
                case "not base64 at all":
                    value = scenario;
                    break;
                case "encrypted with other keys":
                    value = Cryptography.Encrypt(ConfiguredKey.ToString(), KeyMaterial.Generate());
                    break;
                default:
                    value = Cryptography.Encrypt(scenario, KeyMaterialTests.TestKeys);
                    break;
            }

            var ex = Assert.Throws<ConfigurationErrorsException>(() => AdminKey.Read(WithAdminKey(value), KeyMaterialTests.TestKeys));

            Assert.That(ex.Message, Does.Contain(AdminKey.Setting));
            Assert.That(ex.Message, Does.Not.Contain(value));
        }
    }
}
