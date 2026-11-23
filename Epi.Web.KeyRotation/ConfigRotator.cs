using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Epi.Web.Common.Security;

namespace Epi.Web.KeyRotation
{
    /// <summary>
    /// Re-encrypts the secrets in a Web.config or app.config: encrypted connection strings, AdminKey,
    /// the key settings themselves and (when present) the token settings.
    /// </summary>
    public static class ConfigRotator
    {
        private const string AppSettingsPath = "/*[local-name()='configuration']/*[local-name()='appSettings']";
        private const string ConnectionStringsPath = "/*[local-name()='configuration']/*[local-name()='connectionStrings']";

        public static XmlDocument Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new KeyRotationException("Configuration file not found: " + path);
            }

            var config = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            config.Load(path);
            return config;
        }

        /// <summary>
        /// Atomically replaces <paramref name="path"/>, keeping its encoding and ACL, and returns the backup path.
        /// Both the backup and the temporary file end in the original extension so IIS won't serve them.
        /// </summary>
        public static string Save(XmlDocument config, string path)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string backup = Path.Combine(directory, name + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".keyrotation-backup" + extension);
            string temp = Path.Combine(directory, name + "." + Guid.NewGuid().ToString("N") + ".keyrotation-tmp" + extension);
            try
            {
                using (var writer = new StreamWriter(temp, false, DetectEncoding(fullPath)))
                {
                    config.Save(writer);
                }

                File.Replace(temp, fullPath, backup);
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }

            return backup;
        }

        /// <summary>
        /// Returns the key material the configuration sets, or null when any key setting is missing or empty
        /// (the application then used its built-in legacy keys).
        /// </summary>
        public static KeyMaterial ReadConfiguredKeys(XmlDocument config)
        {
            NameValueCollection settings = ReadAppSettings(config);
            string passphrase = settings[KeyMaterial.PassphraseSetting];
            string salt = settings[KeyMaterial.SaltSetting];
            string vector = settings[KeyMaterial.VectorSetting];
            if (string.IsNullOrEmpty(passphrase) || string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(vector))
            {
                return null;
            }

            try
            {
                return new KeyMaterial(passphrase, salt, vector);
            }
            catch (ArgumentException)
            {
                throw new KeyRotationException("The configuration's " + KeyMaterial.VectorSetting + " isn't 16 ASCII characters.");
            }
        }

        public static NameValueCollection ReadAppSettings(XmlDocument config)
        {
            var settings = new NameValueCollection(StringComparer.Ordinal);
            XmlElement appSettings = (XmlElement)config.SelectSingleNode(AppSettingsPath);
            if (appSettings == null)
            {
                return settings;
            }

            foreach (XmlNode node in appSettings.ChildNodes)
            {
                var element = node as XmlElement;
                if (element == null)
                {
                    continue;
                }

                if (element.LocalName == "add")
                {
                    settings[element.GetAttribute("key")] = element.GetAttribute("value");
                }
                else if (element.LocalName == "remove")
                {
                    settings.Remove(element.GetAttribute("key"));
                }
                else if (element.LocalName == "clear")
                {
                    settings.Clear();
                }
            }

            return settings;
        }

        public static bool SameKeys(KeyMaterial first, KeyMaterial second)
        {
            return string.Equals(first.Passphrase, second.Passphrase, StringComparison.Ordinal) &&
                   string.Equals(first.Salt, second.Salt, StringComparison.Ordinal) &&
                   string.Equals(first.Vector, second.Vector, StringComparison.Ordinal);
        }

        /// <summary>
        /// Rotates every secret in <paramref name="config"/> in memory and validates the result the way the
        /// application does at startup. Returns a report that names items but never contains values.
        /// </summary>
        public static IList<string> Rotate(XmlDocument config, KeyMaterial oldKeys, KeyFile newKeys)
        {
            if (oldKeys == null) throw new ArgumentNullException(nameof(oldKeys));
            if (newKeys == null) throw new ArgumentNullException(nameof(newKeys));
            if (SameKeys(oldKeys, newKeys.ConnectionStringKeys))
            {
                throw new KeyRotationException("The old and new key material are the same.");
            }

            RejectExternalSections(config);
            var report = new List<string>();
            RotateConnectionStrings(config, oldKeys, newKeys.ConnectionStringKeys, report);
            RotateAdminKey(config, oldKeys, newKeys, report);

            XmlElement appSettings = GetOrCreateAppSettings(config);
            SetAppSetting(appSettings, KeyMaterial.PassphraseSetting, newKeys.ConnectionStringKeys.Passphrase);
            SetAppSetting(appSettings, KeyMaterial.SaltSetting, newKeys.ConnectionStringKeys.Salt);
            SetAppSetting(appSettings, KeyMaterial.VectorSetting, newKeys.ConnectionStringKeys.Vector);
            report.Add("appSettings/KeyForConnectionString*: set to the new key material");

            RotateTokenSettings(config, appSettings, newKeys, report);

            try
            {
                Cryptography.ValidateConfiguration(ReadAppSettings(config));
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                throw new KeyRotationException("The rotated configuration doesn't validate: " + ex.Message);
            }

            return report;
        }

        /// <summary>
        /// Decrypts the named connection string (with the old or new keys) and returns the ADO.NET connection string,
        /// unwrapping an Entity Framework "provider connection string".
        /// </summary>
        public static string GetDatabaseConnectionString(XmlDocument config, string name, KeyMaterial oldKeys, KeyMaterial newKeys)
        {
            XmlElement element = null;
            XmlNode section = config.SelectSingleNode(ConnectionStringsPath);
            if (section != null)
            {
                foreach (XmlNode node in section.ChildNodes)
                {
                    var candidate = node as XmlElement;
                    if (candidate != null && candidate.LocalName == "add" && candidate.GetAttribute("name") == name)
                    {
                        element = candidate;
                    }
                }
            }

            if (element == null)
            {
                throw new KeyRotationException("Connection string " + name + " isn't in the configuration.");
            }

            string plainText;
            string value = element.GetAttribute("connectionString");
            if (!TryDecryptConnectionString(value, oldKeys, out plainText) && !TryDecryptConnectionString(value, newKeys, out plainText))
            {
                throw new KeyRotationException("Connection string " + name + " can't be decrypted with the old or new keys.");
            }

            var builder = new DbConnectionStringBuilder { ConnectionString = plainText };
            object provider;
            if (builder.TryGetValue("provider connection string", out provider) && provider != null)
            {
                return provider.ToString();
            }

            return plainText;
        }

        private static void RejectExternalSections(XmlDocument config)
        {
            foreach (string path in new[] { AppSettingsPath, ConnectionStringsPath })
            {
                var section = config.SelectSingleNode(path) as XmlElement;
                if (section != null && (section.HasAttribute("configSource") || section.HasAttribute("file")))
                {
                    throw new KeyRotationException(
                        section.LocalName + " is stored in a separate file (configSource/file). Rotate that file's settings in place first.");
                }
            }
        }

        private static void RotateConnectionStrings(XmlDocument config, KeyMaterial oldKeys, KeyMaterial newKeys, List<string> report)
        {
            XmlNode section = config.SelectSingleNode(ConnectionStringsPath);
            if (section == null)
            {
                return;
            }

            foreach (XmlNode node in section.ChildNodes)
            {
                var element = node as XmlElement;
                if (element == null || element.LocalName != "add")
                {
                    continue;
                }

                string name = element.GetAttribute("name");
                string value = element.GetAttribute("connectionString");
                string plainText;
                if (string.IsNullOrWhiteSpace(value))
                {
                    report.Add("connectionStrings/" + name + ": empty, unchanged");
                }
                else if (TryDecryptConnectionString(value, oldKeys, out plainText))
                {
                    element.SetAttribute("connectionString", Cryptography.Encrypt(plainText, newKeys));
                    report.Add("connectionStrings/" + name + ": re-encrypted");
                }
                else if (TryDecryptConnectionString(value, newKeys, out plainText))
                {
                    report.Add("connectionStrings/" + name + ": already uses the new keys");
                }
                else if (LooksEncrypted(value))
                {
                    throw new KeyRotationException(
                        "Connection string " + name + " looks encrypted but can't be decrypted with the old or new keys. " +
                        "Check --old-keys or the deployment's key settings. Nothing was written.");
                }
                else
                {
                    report.Add("connectionStrings/" + name + ": not encrypted with the old keys (plain text?), unchanged");
                }
            }
        }

        /// <summary>
        /// True for Base64 that decodes to whole AES blocks. Plain-text connection strings contain ';', '=' or other
        /// characters where Base64 doesn't allow them, so they never match.
        /// </summary>
        private static bool LooksEncrypted(string value)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(value.Trim());
                return bytes.Length > 0 && bytes.Length % 16 == 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void RotateAdminKey(XmlDocument config, KeyMaterial oldKeys, KeyFile newKeys, List<string> report)
        {
            XmlElement element = FindAppSetting(config.SelectSingleNode(AppSettingsPath) as XmlElement, AdminKey.Setting);
            string value = element == null ? null : element.GetAttribute("value");
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Add("appSettings/AdminKey: not set, admin operations stay disabled");
                return;
            }

            string plainText;
            bool usesNewKeys = false;
            if (!OrganizationKeyPlan.TryDecryptGuidText(value, oldKeys, out plainText))
            {
                if (!OrganizationKeyPlan.TryDecryptGuidText(value, newKeys.ConnectionStringKeys, out plainText))
                {
                    throw new KeyRotationException("AdminKey can't be decrypted to a GUID with the old or new keys.");
                }

                usesNewKeys = true;
            }

            Guid current = Guid.Parse(plainText);
            Guid target;
            if (newKeys.AdminKeyGuid.HasValue)
            {
                target = newKeys.AdminKeyGuid.Value;
            }
            else if (KnownPublicValues.IsKnown(current) || current == Guid.Empty)
            {
                throw new KeyRotationException(
                    "AdminKey is a publicly known value. Add " + KeyFile.AdminKeyGuidSetting + " to the new key file (generate creates one).");
            }
            else
            {
                target = current;
            }

            if (usesNewKeys && target == current)
            {
                report.Add("appSettings/AdminKey: already uses the new keys");
                return;
            }

            element.SetAttribute("value", Cryptography.Encrypt(target.ToString("D"), newKeys.ConnectionStringKeys));
            report.Add(target == current
                ? "appSettings/AdminKey: re-encrypted (same admin key)"
                : "appSettings/AdminKey: replaced with the key file's " + KeyFile.AdminKeyGuidSetting);
        }

        private static void RotateTokenSettings(XmlDocument config, XmlElement appSettings, KeyFile newKeys, List<string> report)
        {
            NameValueCollection settings = ReadAppSettings(config);
            if (!KeyMaterial.AnySettingPresent(settings, KeyMaterial.TokenPassphraseSetting, KeyMaterial.TokenSaltSetting, KeyMaterial.TokenVectorSetting))
            {
                report.Add("appSettings/Token*: not configured, unchanged");
                return;
            }

            if (newKeys.TokenKeys == null)
            {
                KeyMaterial.FromSettings(settings, KeyMaterial.TokenPassphraseSetting, KeyMaterial.TokenSaltSetting, KeyMaterial.TokenVectorSetting);
                report.Add("appSettings/Token*: the key file has no token settings, unchanged");
                return;
            }

            SetAppSetting(appSettings, KeyMaterial.TokenPassphraseSetting, newKeys.TokenKeys.Passphrase);
            SetAppSetting(appSettings, KeyMaterial.TokenSaltSetting, newKeys.TokenKeys.Salt);
            SetAppSetting(appSettings, KeyMaterial.TokenVectorSetting, newKeys.TokenKeys.Vector);
            report.Add("appSettings/Token*: set to the new token settings; update the application that receives the records-viewer link to match");
        }

        private static bool TryDecryptConnectionString(string cipherText, KeyMaterial keys, out string plainText)
        {
            plainText = null;
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                return false;
            }

            string candidate;
            try
            {
                candidate = Cryptography.Decrypt(cipherText.Trim(), keys);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }

            foreach (char c in candidate)
            {
                if (c == '\uFFFD' || (char.IsControl(c) && c != '\t'))
                {
                    return false;
                }
            }

            try
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = candidate };
                if (builder.Count == 0)
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }

            plainText = candidate;
            return true;
        }

        private static XmlElement GetOrCreateAppSettings(XmlDocument config)
        {
            var appSettings = config.SelectSingleNode(AppSettingsPath) as XmlElement;
            if (appSettings != null)
            {
                return appSettings;
            }

            XmlElement root = config.DocumentElement;
            appSettings = config.CreateElement("appSettings", root.NamespaceURI);
            root.PrependChild(appSettings);
            return appSettings;
        }

        private static XmlElement FindAppSetting(XmlElement appSettings, string key)
        {
            XmlElement found = null;
            if (appSettings == null)
            {
                return null;
            }

            foreach (XmlNode node in appSettings.ChildNodes)
            {
                var element = node as XmlElement;
                if (element != null && element.LocalName == "add" && element.GetAttribute("key") == key)
                {
                    found = element;
                }
            }

            return found;
        }

        private static void SetAppSetting(XmlElement appSettings, string key, string value)
        {
            XmlElement existing = FindAppSetting(appSettings, key);
            if (existing != null)
            {
                existing.SetAttribute("value", value);
                return;
            }

            XmlDocument config = appSettings.OwnerDocument;
            XmlElement added = config.CreateElement("add", appSettings.NamespaceURI);
            added.SetAttribute("key", key);
            added.SetAttribute("value", value);

            XmlElement last = null;
            foreach (XmlNode node in appSettings.ChildNodes)
            {
                if (node is XmlElement)
                {
                    last = (XmlElement)node;
                }
            }

            if (last == null)
            {
                appSettings.AppendChild(added);
                return;
            }

            XmlNode indent = last.PreviousSibling;
            appSettings.InsertAfter(added, last);
            if (indent != null && indent.NodeType == XmlNodeType.Whitespace)
            {
                appSettings.InsertAfter(config.CreateWhitespace(indent.Value), last);
            }
        }

        private static Encoding DetectEncoding(string path)
        {
            var bom = new byte[3];
            int read;
            using (FileStream stream = File.OpenRead(path))
            {
                read = stream.Read(bom, 0, bom.Length);
            }

            if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            {
                return new UnicodeEncoding(false, true);
            }

            return new UTF8Encoding(read == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF);
        }
    }
}
