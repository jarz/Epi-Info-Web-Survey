using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;

namespace Epi.Web.Common.Security
{
    /// <summary>
    /// Reads the encrypted AdminKey appSetting and compares supplied admin keys against it.
    /// </summary>
    public static class AdminKey
    {
        public const string Setting = "AdminKey";

        /// <summary>
        /// Returns the configured admin key, or null when the setting is absent (admin operations disabled).
        /// Throws when the setting is present but unusable or publicly known.
        /// </summary>
        public static Guid? Read(NameValueCollection settings, KeyMaterial keys)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            string cipherText = settings[Setting];
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                return null;
            }

            if (KnownPublicValues.IsKnown(cipherText))
            {
                throw KnownValue();
            }

            string plainText;
            try
            {
                plainText = Cryptography.Decrypt(cipherText.Trim(), keys);
            }
            catch (System.Exception ex) when (ex is FormatException || ex is CryptographicException)
            {
                throw new ConfigurationErrorsException(
                    Setting + " can't be decrypted with the configured key material. Re-encrypt it with Epi.Web.KeyRotation.");
            }

            Guid key;
            if (!Guid.TryParse(plainText, out key) || key == Guid.Empty)
            {
                throw new ConfigurationErrorsException(Setting + " must decrypt to a non-empty GUID.");
            }

            if (KnownPublicValues.IsKnown(key))
            {
                throw KnownValue();
            }

            return key;
        }

        public static bool Matches(string suppliedKey, Guid? configuredKey)
        {
            Guid supplied;
            if (!configuredKey.HasValue || !Guid.TryParse(suppliedKey, out supplied) || supplied == Guid.Empty)
            {
                return false;
            }

            byte[] expected = configuredKey.Value.ToByteArray();
            byte[] actual = supplied.ToByteArray();
            int difference = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                difference |= expected[i] ^ actual[i];
            }

            return difference == 0;
        }

        private static ConfigurationErrorsException KnownValue()
        {
            return new ConfigurationErrorsException(
                Setting + " is a publicly known value. Generate a new admin key with Epi.Web.KeyRotation.");
        }
    }
}
