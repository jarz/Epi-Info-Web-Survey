using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Epi.Web.Common.Security;
using Epi.Web.KeyRotation;
using NUnit.Framework;

namespace Epi.Web.MVC.Test.Security
{
    [TestFixture]
    public class KeyRotationTests
    {
        private const string EntityConnection =
            "metadata=res://*/EF.csdl|res://*/EF.ssdl|res://*/EF.msl;provider=System.Data.SqlClient;" +
            "provider connection string=\"Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Synthetic;Integrated Security=True\"";

        private const string AdoConnection = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Synthetic;Integrated Security=True";
        private const string MembershipConnection = "data source=.\\SQLEXPRESS;Integrated Security=SSPI";
        private static readonly Guid ExistingAdminKey = new Guid("c0ffee00-1234-4abc-8def-0123456789ab");

        private string directory;

        [SetUp]
        public void CreateDirectory()
        {
            directory = Path.Combine(Path.GetTempPath(), "Epi.Web.KeyRotation.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void DeleteDirectory()
        {
            Directory.Delete(directory, true);
        }

        private static string LegacyConfigXml(KeyMaterial keys, bool includeKeySettings)
        {
            string keySettings = includeKeySettings
                ? "    <add key=\"KeyForConnectionStringPassphrase\" value=\"" + keys.Passphrase + "\" />\r\n" +
                  "    <add key=\"KeyForConnectionStringSalt\" value=\"" + keys.Salt + "\" />\r\n" +
                  "    <add key=\"KeyForConnectionStringVector\" value=\"" + keys.Vector + "\" />\r\n"
                : string.Empty;
            return "<?xml version=\"1.0\"?>\r\n<configuration>\r\n  <connectionStrings>\r\n" +
                   "    <add name=\"EIWSEntities\" connectionString=\"" + Cryptography.Encrypt(EntityConnection, keys) + "\" />\r\n" +
                   "    <add name=\"EIWSADO\" connectionString=\"" + Cryptography.Encrypt(AdoConnection, keys) + "\" />\r\n" +
                   "    <add name=\"ApplicationServices\" connectionString=\"" + MembershipConnection + "\" />\r\n" +
                   "  </connectionStrings>\r\n  <appSettings>\r\n" +
                   "    <add key=\"URL\" value=\"http://localhost/\" />\r\n" + keySettings +
                   "    <add key=\"AdminKey\" value=\"" + Cryptography.Encrypt(ExistingAdminKey.ToString("D"), keys) + "\" />\r\n" +
                   "  </appSettings>\r\n</configuration>\r\n";
        }

        private static XmlDocument Load(string xml)
        {
            var config = new XmlDocument { PreserveWhitespace = true };
            config.LoadXml(xml);
            return config;
        }

        private static string ConnectionString(XmlDocument config, string name)
        {
            return ((XmlElement)config.SelectSingleNode("/configuration/connectionStrings/add[@name='" + name + "']")).GetAttribute("connectionString");
        }

        private static KeyFile KeyFileWithoutAdminKey(KeyMaterial keys)
        {
            return KeyFile.ParseNew(new[]
            {
                KeyMaterial.PassphraseSetting + "=" + keys.Passphrase,
                KeyMaterial.SaltSetting + "=" + keys.Salt,
                KeyMaterial.VectorSetting + "=" + keys.Vector,
            });
        }

        [Test]
        public void Rotate_reencrypts_a_configuration_that_used_the_built_in_keys()
        {
            XmlDocument config = Load(LegacyConfigXml(LegacyKeys.ConnectionStringKeys, false));
            KeyFile newKeys = KeyFile.Generate();

            IList<string> report = ConfigRotator.Rotate(config, LegacyKeys.ConnectionStringKeys, newKeys);

            KeyMaterial configured = ConfigRotator.ReadConfiguredKeys(config);
            Assert.That(ConfigRotator.SameKeys(configured, newKeys.ConnectionStringKeys), Is.True);
            Assert.That(Cryptography.Decrypt(ConnectionString(config, "EIWSEntities"), configured), Is.EqualTo(EntityConnection));
            Assert.That(Cryptography.Decrypt(ConnectionString(config, "EIWSADO"), configured), Is.EqualTo(AdoConnection));
            Assert.That(ConnectionString(config, "ApplicationServices"), Is.EqualTo(MembershipConnection));
            Assert.That(AdminKey.Read(ConfigRotator.ReadAppSettings(config), configured), Is.EqualTo(newKeys.AdminKeyGuid));
            Assert.DoesNotThrow(() => Cryptography.ValidateConfiguration(ConfigRotator.ReadAppSettings(config)));
            Assert.That(report, Has.Some.Contains("ApplicationServices: not encrypted with the old keys"));
        }

        [Test]
        public void Rotate_keeps_a_private_admin_key_when_the_key_file_has_none()
        {
            KeyMaterial oldKeys = KeyMaterial.Generate();
            XmlDocument config = Load(LegacyConfigXml(oldKeys, true));
            KeyMaterial newKeys = KeyMaterial.Generate();

            ConfigRotator.Rotate(config, oldKeys, KeyFileWithoutAdminKey(newKeys));

            Assert.That(AdminKey.Read(ConfigRotator.ReadAppSettings(config), newKeys), Is.EqualTo(ExistingAdminKey));
        }

        [Test]
        public void Rotate_recognizes_values_that_already_use_the_new_keys()
        {
            XmlDocument config = Load(LegacyConfigXml(LegacyKeys.ConnectionStringKeys, false));
            KeyFile newKeys = KeyFile.Generate();
            ConfigRotator.Rotate(config, LegacyKeys.ConnectionStringKeys, newKeys);
            string rotatedXml = config.OuterXml;

            IList<string> report = ConfigRotator.Rotate(config, LegacyKeys.ConnectionStringKeys, newKeys);

            Assert.That(config.OuterXml, Is.EqualTo(rotatedXml));
            Assert.That(report.Count(line => line.Contains("already uses the new keys")), Is.EqualTo(3));
        }

        [Test]
        public void Rotate_refuses_sections_stored_in_another_file()
        {
            XmlDocument config = Load("<configuration><appSettings configSource=\"secrets.config\" /></configuration>");

            Assert.Throws<KeyRotationException>(() => ConfigRotator.Rotate(config, LegacyKeys.ConnectionStringKeys, KeyFile.Generate()));
        }

        [Test]
        public void Rotate_refuses_a_connection_string_encrypted_with_unknown_keys()
        {
            XmlDocument config = Load(LegacyConfigXml(KeyMaterial.Generate(), false));

            var ex = Assert.Throws<KeyRotationException>(() => ConfigRotator.Rotate(config, KeyMaterial.Generate(), KeyFile.Generate()));

            Assert.That(ex.Message, Does.Contain("EIWSEntities"));
        }

        [Test]
        public void Rotate_command_refuses_old_keys_that_hold_the_new_key_material()
        {
            string configPath = Path.Combine(directory, "Web.config");
            string keyPath = Path.Combine(directory, "new.keys");
            string oldKeyPath = Path.Combine(directory, "old.keys");
            File.WriteAllText(configPath, LegacyConfigXml(KeyMaterial.Generate(), true));
            byte[] original = File.ReadAllBytes(configPath);
            Run("generate", "--out", keyPath);
            KeyMaterial newKeys = KeyFile.ReadNew(keyPath).ConnectionStringKeys;
            File.WriteAllLines(oldKeyPath, new[]
            {
                KeyMaterial.PassphraseSetting + "=" + newKeys.Passphrase,
                KeyMaterial.SaltSetting + "=" + newKeys.Salt,
                KeyMaterial.VectorSetting + "=" + newKeys.Vector,
            });

            CommandResult result = Run("rotate", "--config", configPath, "--new-keys", keyPath, "--old-keys", oldKeyPath, "--apply");

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Error, Does.Contain("--old-keys"));
            Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(original));
        }

        [Test]
        public void Rotate_command_accepts_built_in_keys_again_once_the_configuration_uses_the_new_keys()
        {
            string configPath = Path.Combine(directory, "Web.config");
            string keyPath = Path.Combine(directory, "new.keys");
            File.WriteAllText(configPath, LegacyConfigXml(LegacyKeys.ConnectionStringKeys, false));
            Run("generate", "--out", keyPath);
            Assert.That(Run("rotate", "--config", configPath, "--new-keys", keyPath, "--legacy-built-in-keys", "--apply").ExitCode, Is.EqualTo(0));
            byte[] rotated = File.ReadAllBytes(configPath);

            CommandResult result = Run("rotate", "--config", configPath, "--new-keys", keyPath, "--legacy-built-in-keys");

            Assert.That(result.ExitCode, Is.EqualTo(0), result.Error);
            Assert.That(result.Output, Does.Contain("EIWSEntities: already uses the new keys"));
            Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(rotated));
        }

        [Test]
        public void Rotate_command_is_a_dry_run_until_apply_and_never_prints_secrets()
        {
            string configPath = Path.Combine(directory, "Web.config");
            string keyPath = Path.Combine(directory, "new.keys");
            File.WriteAllText(configPath, LegacyConfigXml(LegacyKeys.ConnectionStringKeys, false));
            byte[] original = File.ReadAllBytes(configPath);
            Assert.That(Run("generate", "--out", keyPath).ExitCode, Is.EqualTo(0));
            KeyFile newKeys = KeyFile.ReadNew(keyPath);
            string[] secrets =
            {
                newKeys.ConnectionStringKeys.Passphrase, newKeys.ConnectionStringKeys.Salt, newKeys.ConnectionStringKeys.Vector,
                newKeys.AdminKeyGuid.Value.ToString("D"), ExistingAdminKey.ToString("D"),
            };

            CommandResult dryRun = Run("rotate", "--config", configPath, "--new-keys", keyPath, "--legacy-built-in-keys");
            CommandResult applied = Run("rotate", "--config", configPath, "--new-keys", keyPath, "--legacy-built-in-keys", "--apply");

            Assert.That(dryRun.ExitCode, Is.EqualTo(0), dryRun.Error);
            Assert.That(applied.ExitCode, Is.EqualTo(0), applied.Error);
            Assert.That(dryRun.Output, Does.Contain("Nothing was written"));
            string[] backups = Directory.GetFiles(directory, "Web.*.keyrotation-backup.config");
            Assert.That(backups, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllBytes(backups[0]), Is.EqualTo(original));
            Assert.That(ConfigRotator.SameKeys(ConfigRotator.ReadConfiguredKeys(ConfigRotator.Load(configPath)), newKeys.ConnectionStringKeys), Is.True);
            foreach (string secret in secrets)
            {
                Assert.That(dryRun.Output + dryRun.Error + applied.Output + applied.Error, Does.Not.Contain(secret));
            }
        }

        [Test]
        public void Rotate_command_requires_old_keys_when_the_configuration_has_none()
        {
            string configPath = Path.Combine(directory, "Web.config");
            string keyPath = Path.Combine(directory, "new.keys");
            File.WriteAllText(configPath, LegacyConfigXml(LegacyKeys.ConnectionStringKeys, false));
            byte[] original = File.ReadAllBytes(configPath);
            Run("generate", "--out", keyPath);

            CommandResult result = Run("rotate", "--config", configPath, "--new-keys", keyPath, "--apply");

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Error, Does.Contain("--legacy-built-in-keys"));
            Assert.That(File.ReadAllBytes(configPath), Is.EqualTo(original));
        }

        [Test]
        public void Generate_command_never_overwrites_an_existing_file()
        {
            string keyPath = Path.Combine(directory, "new.keys");
            File.WriteAllText(keyPath, "keep me");

            CommandResult result = Run("generate", "--out", keyPath);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(File.ReadAllText(keyPath), Is.EqualTo("keep me"));
        }

        [Test]
        public void Plan_reencrypts_organization_keys_and_keeps_their_exact_text()
        {
            KeyMaterial oldKeys = LegacyKeys.ConnectionStringKeys;
            KeyMaterial newKeys = KeyMaterial.Generate();
            const string upperCaseKey = "A1B2C3D4-0000-4000-8000-000000000001";
            const string lowerCaseKey = "a1b2c3d4-0000-4000-8000-000000000002";
            var rows = new List<OrganizationKeyRow>
            {
                new OrganizationKeyRow(1, Cryptography.Encrypt(upperCaseKey, oldKeys)),
                new OrganizationKeyRow(2, Cryptography.Encrypt(lowerCaseKey, oldKeys)),
                new OrganizationKeyRow(3, Cryptography.Encrypt("a1b2c3d4-0000-4000-8000-000000000003", newKeys)),
                new OrganizationKeyRow(4, null),
            };

            OrganizationKeyPlan plan = OrganizationKeyPlan.Create(rows, oldKeys, newKeys);

            Assert.That(plan.Updates.Select(u => u.OrganizationId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(plan.AlreadyCurrent, Is.EqualTo(1));
            Assert.That(plan.Empty, Is.EqualTo(1));
            Assert.That(Cryptography.Decrypt(plan.Updates[0].NewValue, newKeys), Is.EqualTo(upperCaseKey));
            Assert.That(plan.Updates[1].NewValue, Is.EqualTo(Cryptography.Encrypt(lowerCaseKey, newKeys)));
            Assert.DoesNotThrow(() => plan.Verify(plan.Updates.Select(u => new OrganizationKeyRow(u.OrganizationId, u.NewValue)), newKeys));
        }

        [Test]
        public void Plan_aborts_on_a_row_neither_key_set_can_decrypt()
        {
            string foreign = Cryptography.Encrypt("a1b2c3d4-0000-4000-8000-000000000009", KeyMaterial.Generate());
            var rows = new[] { new OrganizationKeyRow(42, foreign) };

            var ex = Assert.Throws<KeyRotationException>(() => OrganizationKeyPlan.Create(rows, LegacyKeys.ConnectionStringKeys, KeyMaterial.Generate()));

            Assert.That(ex.Message, Does.Contain("OrganizationId 42"));
            Assert.That(ex.Message, Does.Not.Contain(foreign));
        }

        [Test]
        public void Plan_verification_fails_when_a_row_was_not_updated()
        {
            KeyMaterial oldKeys = LegacyKeys.ConnectionStringKeys;
            KeyMaterial newKeys = KeyMaterial.Generate();
            var row = new OrganizationKeyRow(7, Cryptography.Encrypt("a1b2c3d4-0000-4000-8000-000000000007", oldKeys));
            OrganizationKeyPlan plan = OrganizationKeyPlan.Create(new[] { row }, oldKeys, newKeys);

            Assert.Throws<KeyRotationException>(() => plan.Verify(new[] { row }, newKeys));
        }

        [Test]
        public void Plan_check_counts_rows_that_use_the_keys_and_rejects_any_that_do_not()
        {
            KeyMaterial keys = KeyMaterial.Generate();
            var current = new List<OrganizationKeyRow>
            {
                new OrganizationKeyRow(1, Cryptography.Encrypt("a1b2c3d4-0000-4000-8000-000000000001", keys)),
                new OrganizationKeyRow(2, null),
            };
            var stale = new List<OrganizationKeyRow>(current)
            {
                new OrganizationKeyRow(3, Cryptography.Encrypt("a1b2c3d4-0000-4000-8000-000000000003", LegacyKeys.ConnectionStringKeys)),
            };

            OrganizationKeyPlan plan = OrganizationKeyPlan.CheckCurrent(current, keys);
            var ex = Assert.Throws<KeyRotationException>(() => OrganizationKeyPlan.CheckCurrent(stale, keys));

            Assert.That(plan.Updates, Is.Empty);
            Assert.That(plan.AlreadyCurrent, Is.EqualTo(1));
            Assert.That(plan.Empty, Is.EqualTo(1));
            Assert.That(ex.Message, Does.Contain("OrganizationId 3").And.Contain("--old-keys"));
        }

        private static CommandResult Run(params string[] args)
        {
            var output = new StringWriter();
            var error = new StringWriter();
            int exitCode = Program.Run(args, new StringReader(string.Empty), output, error);
            return new CommandResult { ExitCode = exitCode, Output = output.ToString(), Error = error.ToString() };
        }

        private sealed class CommandResult
        {
            public int ExitCode { get; set; }

            public string Output { get; set; }

            public string Error { get; set; }
        }
    }
}
