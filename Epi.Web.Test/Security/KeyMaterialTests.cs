using System.Collections.Specialized;
using System.Configuration;
using Epi.Web.Common.Security;
using Epi.Web.KeyRotation;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class KeyMaterialTests
    {
        internal static readonly KeyMaterial TestKeys = new KeyMaterial("test-only-passphrase-7c1d9e4b2a6f", "test-only-salt-5e8a", "TestOnlyIV-4k9Q2");

        internal static NameValueCollection Settings(KeyMaterial keys)
        {
            return new NameValueCollection
            {
                { KeyMaterial.PassphraseSetting, keys.Passphrase },
                { KeyMaterial.SaltSetting, keys.Salt },
                { KeyMaterial.VectorSetting, keys.Vector },
            };
        }

        [Test]
        public void Encrypt_matches_the_algorithm_existing_deployments_used()
        {
            // Produced by the previous Cryptography implementation (PasswordDeriveBytes/MD5, AES-128-CBC) with these keys.
            const string cipherText = "Qkti01k8zmLJs0f3kXGdo9vhHf6R1zt2KwwWwdekvn5I94tod1GTJZnKdSHph7yv";

            Assert.That(Cryptography.Encrypt("3f2504e0-4f89-11d3-9a0c-0305e82c3301", TestKeys), Is.EqualTo(cipherText));
            Assert.That(Cryptography.Decrypt(cipherText, TestKeys), Is.EqualTo("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));
        }

        [TestCase("")]
        [TestCase("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Synthetic;Integrated Security=True")]
        [TestCase("Données synthétiques ✓")]
        public void Encrypt_round_trips_with_generated_keys(string plainText)
        {
            KeyMaterial keys = KeyMaterial.Generate();

            Assert.That(Cryptography.Decrypt(Cryptography.Encrypt(plainText, keys), keys), Is.EqualTo(plainText));
        }

        [Test]
        public void Decrypt_with_other_keys_throws_a_cryptographic_exception()
        {
            string cipherText = Cryptography.Encrypt("3f2504e0-4f89-11d3-9a0c-0305e82c3301", TestKeys);

            Assert.Throws<System.Security.Cryptography.CryptographicException>(() => Cryptography.Decrypt(cipherText, KeyMaterial.Generate()));
        }

        [Test]
        public void FromSettings_names_every_missing_setting()
        {
            var settings = new NameValueCollection { { KeyMaterial.SaltSetting, "  " } };

            var ex = Assert.Throws<ConfigurationErrorsException>(() => KeyMaterial.FromSettings(settings));

            Assert.That(ex.Message, Does.Contain(KeyMaterial.PassphraseSetting));
            Assert.That(ex.Message, Does.Contain(KeyMaterial.SaltSetting));
            Assert.That(ex.Message, Does.Contain(KeyMaterial.VectorSetting));
        }

        [Test]
        public void FromSettings_rejects_the_former_built_in_keys_without_echoing_them()
        {
            KeyMaterial legacy = LegacyKeys.ConnectionStringKeys;

            var ex = Assert.Throws<ConfigurationErrorsException>(() => KeyMaterial.FromSettings(Settings(legacy)));

            Assert.That(ex.Message, Does.Contain("publicly known"));
            Assert.That(ex.Message, Does.Not.Contain(legacy.Passphrase));
            Assert.That(ex.Message, Does.Not.Contain(legacy.Salt));
            Assert.That(ex.Message, Does.Not.Contain(legacy.Vector));
        }

        [Test]
        public void FromSettings_rejects_one_publicly_known_value_mixed_with_new_ones()
        {
            KeyMaterial generated = KeyMaterial.Generate();
            var mixed = new KeyMaterial(generated.Passphrase, generated.Salt, LegacyKeys.ConnectionStringKeys.Vector);

            Assert.Throws<ConfigurationErrorsException>(() => KeyMaterial.FromSettings(Settings(mixed)));
        }

        [TestCase("short-vector")]
        [TestCase("seventeen-chars-x")]
        [TestCase("sixteen-chars-é!")]
        public void FromSettings_requires_a_16_character_ascii_vector(string vector)
        {
            NameValueCollection settings = Settings(TestKeys);
            settings[KeyMaterial.VectorSetting] = vector;

            var ex = Assert.Throws<ConfigurationErrorsException>(() => KeyMaterial.FromSettings(settings));

            Assert.That(ex.Message, Does.Contain(KeyMaterial.VectorSetting));
        }

        [Test]
        public void Generate_creates_distinct_keys_that_configuration_accepts()
        {
            KeyMaterial first = KeyMaterial.Generate();
            KeyMaterial second = KeyMaterial.Generate();

            Assert.That(first.Passphrase, Has.Length.EqualTo(64));
            Assert.That(first.Vector, Has.Length.EqualTo(KeyMaterial.VectorLength));
            Assert.That(first.Passphrase, Is.Not.EqualTo(second.Passphrase));
            Assert.That(KeyMaterial.FromSettings(Settings(first)).Salt, Is.EqualTo(first.Salt));
        }

        [Test]
        public void ValidateConfiguration_requires_complete_token_settings_once_any_is_set()
        {
            NameValueCollection settings = Settings(KeyMaterial.Generate());
            settings[KeyMaterial.TokenPassphraseSetting] = "only-one-token-setting";

            var ex = Assert.Throws<ConfigurationErrorsException>(() => Cryptography.ValidateConfiguration(settings));

            Assert.That(ex.Message, Does.Contain(KeyMaterial.TokenSaltSetting));
            Assert.That(ex.Message, Does.Contain(KeyMaterial.TokenVectorSetting));
        }
    }
}
