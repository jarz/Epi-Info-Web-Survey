using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Epi.Web.Common.Security
{
    /// <summary>
    /// Passphrase, salt and initialization vector used by <see cref="Cryptography"/>.
    /// </summary>
    public sealed class KeyMaterial
    {
        public const string PassphraseSetting = "KeyForConnectionStringPassphrase";
        public const string SaltSetting = "KeyForConnectionStringSalt";
        public const string VectorSetting = "KeyForConnectionStringVector";
        public const string TokenPassphraseSetting = "TokenPassPhrase";
        public const string TokenSaltSetting = "TokenSaltValue";
        public const string TokenVectorSetting = "TokenInitVector";
        public const int VectorLength = 16;

        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        public KeyMaterial(string passphrase, string salt, string vector)
        {
            if (string.IsNullOrEmpty(passphrase))
            {
                throw new ArgumentException("A passphrase is required.", nameof(passphrase));
            }

            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("A salt is required.", nameof(salt));
            }

            if (!IsValidVector(vector))
            {
                throw new ArgumentException("The initialization vector must be exactly 16 ASCII characters.", nameof(vector));
            }

            Passphrase = passphrase;
            Salt = salt;
            Vector = vector;
        }

        public string Passphrase { get; }

        public string Salt { get; }

        public string Vector { get; }

        public bool ContainsKnownPublicValue
        {
            get { return KnownPublicValues.IsKnown(Passphrase) || KnownPublicValues.IsKnown(Salt) || KnownPublicValues.IsKnown(Vector); }
        }

        /// <summary>
        /// Reads the connection-string key settings and rejects missing, malformed or publicly known values.
        /// </summary>
        public static KeyMaterial FromSettings(NameValueCollection settings)
        {
            return FromSettings(settings, PassphraseSetting, SaltSetting, VectorSetting);
        }

        public static KeyMaterial FromSettings(NameValueCollection settings, string passphraseSetting, string saltSetting, string vectorSetting)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var missing = new List<string>();
            string passphrase = settings[passphraseSetting];
            string salt = settings[saltSetting];
            string vector = settings[vectorSetting];
            if (string.IsNullOrWhiteSpace(passphrase)) missing.Add(passphraseSetting);
            if (string.IsNullOrWhiteSpace(salt)) missing.Add(saltSetting);
            if (string.IsNullOrWhiteSpace(vector)) missing.Add(vectorSetting);
            if (missing.Count > 0)
            {
                throw new ConfigurationErrorsException(
                    "Missing required appSettings: " + string.Join(", ", missing) +
                    ". Generate deployment-specific values with Epi.Web.KeyRotation.");
            }

            if (!IsValidVector(vector))
            {
                throw new ConfigurationErrorsException(vectorSetting + " must be exactly 16 ASCII characters.");
            }

            var keys = new KeyMaterial(passphrase, salt, vector);
            if (keys.ContainsKnownPublicValue)
            {
                throw new ConfigurationErrorsException(
                    passphraseSetting + ", " + saltSetting + " and " + vectorSetting +
                    " must be replaced because at least one is a publicly known value. Generate new values and " +
                    "re-encrypt existing data with Epi.Web.KeyRotation.");
            }

            return keys;
        }

        public static bool AnySettingPresent(NameValueCollection settings, params string[] names)
        {
            foreach (string name in names)
            {
                if (!string.IsNullOrWhiteSpace(settings[name]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates random key material: a 64-character passphrase, a 32-character salt and a 16-character vector.
        /// </summary>
        public static KeyMaterial Generate()
        {
            return new KeyMaterial(RandomString(64), RandomString(32), RandomString(VectorLength));
        }

        internal byte[] DeriveKey()
        {
            using (var password = new PasswordDeriveBytes(Passphrase, Encoding.ASCII.GetBytes(Salt), "MD5", 1))
            {
                return password.GetBytes(16);
            }
        }

        internal byte[] VectorBytes()
        {
            return Encoding.ASCII.GetBytes(Vector);
        }

        private static bool IsValidVector(string vector)
        {
            if (vector == null || vector.Length != VectorLength)
            {
                return false;
            }

            foreach (char c in vector)
            {
                if (c > 0x7F)
                {
                    return false;
                }
            }

            return true;
        }

        private static string RandomString(int length)
        {
            var result = new StringBuilder(length);
            var buffer = new byte[1];
            using (var rng = RandomNumberGenerator.Create())
            {
                int limit = 256 - (256 % Alphabet.Length);
                while (result.Length < length)
                {
                    rng.GetBytes(buffer);
                    if (buffer[0] < limit)
                    {
                        result.Append(Alphabet[buffer[0] % Alphabet.Length]);
                    }
                }
            }

            return result.ToString();
        }
    }
}
