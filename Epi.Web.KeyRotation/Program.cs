using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using Epi.Web.Common.Security;

namespace Epi.Web.KeyRotation
{
    public static class Program
    {
        private const string Usage =
@"Epi.Web.KeyRotation - deployment key material for Epi Info Web Survey

  generate --out <key file>
      Creates random key settings, an admin key GUID and token settings in a new file
      readable only by the current user. Never overwrites an existing file.

  encrypt --keys <key file> [--admin-key]
      Encrypts one line from standard input (for example a connection string) with the
      key file's settings and prints the result. With --admin-key, prints the encrypted
      AdminKey value for the key file's AdminKeyGuid instead.

  rotate --config <Web.config> --new-keys <key file>
         [--old-keys <key file> | --legacy-built-in-keys]
         [--database <connection string name>] [--apply]
      Re-encrypts the configuration's encrypted connection strings and AdminKey and sets the
      new key settings. Old keys come from the configuration unless --old-keys is given;
      use --legacy-built-in-keys when the configuration has no key settings.
      --database also re-encrypts dbo.Organization.OrganizationKey through that connection
      string (run it once per database). Without --apply nothing is written.
      Stop the application pools first; the database is updated before the configuration.";

        public static int Main(string[] args)
        {
            return Run(args, Console.In, Console.Out, Console.Error);
        }

        public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
        {
            if (args == null || args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "/?")
            {
                output.WriteLine(Usage);
                return args == null || args.Length == 0 ? 1 : 0;
            }

            try
            {
                Dictionary<string, string> options = ParseOptions(args);
                switch (args[0].ToLowerInvariant())
                {
                    case "generate":
                        return Generate(options, output);
                    case "encrypt":
                        return Encrypt(options, input, output);
                    case "rotate":
                        return Rotate(options, output);
                    default:
                        throw new KeyRotationException("Unknown command '" + args[0] + "'. Run with --help.");
                }
            }
            catch (KeyRotationException ex)
            {
                return Fail(error, ex.Message);
            }
            catch (ConfigurationErrorsException ex)
            {
                return Fail(error, ex.Message);
            }
            catch (XmlException ex)
            {
                return Fail(error, "The configuration isn't valid XML: " + ex.Message);
            }
            catch (SqlException ex)
            {
                return Fail(error, "Database error; no rows were changed: " + ex.Message);
            }
            catch (IOException ex)
            {
                return Fail(error, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Fail(error, ex.Message);
            }
        }

        private static int Generate(Dictionary<string, string> options, TextWriter output)
        {
            string path = Required(options, "out");
            KeyFile.Generate().WriteNew(path);
            output.WriteLine("Wrote new key material to " + Path.GetFullPath(path) + " (readable only by the current user).");
            output.WriteLine("Keep it secret, store the values in your secret store, and delete the file afterwards.");
            return 0;
        }

        private static int Encrypt(Dictionary<string, string> options, TextReader input, TextWriter output)
        {
            KeyFile keys = KeyFile.ReadNew(Required(options, "keys"));
            string plainText;
            if (options.ContainsKey("admin-key"))
            {
                if (!keys.AdminKeyGuid.HasValue)
                {
                    throw new KeyRotationException("The key file has no " + KeyFile.AdminKeyGuidSetting + ".");
                }

                plainText = keys.AdminKeyGuid.Value.ToString("D");
            }
            else
            {
                plainText = input.ReadLine();
                if (string.IsNullOrEmpty(plainText))
                {
                    throw new KeyRotationException("Nothing to encrypt; pass the value on standard input.");
                }
            }

            output.WriteLine(Cryptography.Encrypt(plainText, keys.ConnectionStringKeys));
            return 0;
        }

        private static int Rotate(Dictionary<string, string> options, TextWriter output)
        {
            string configPath = Required(options, "config");
            KeyFile newKeys = KeyFile.ReadNew(Required(options, "new-keys"));
            bool apply = options.ContainsKey("apply");
            bool legacy = options.ContainsKey("legacy-built-in-keys");
            string oldKeysPath;
            options.TryGetValue("old-keys", out oldKeysPath);
            string database;
            options.TryGetValue("database", out database);

            XmlDocument config = ConfigRotator.Load(configPath);
            KeyMaterial configured = ConfigRotator.ReadConfiguredKeys(config);
            KeyMaterial oldKeys;
            if (oldKeysPath != null && legacy)
            {
                throw new KeyRotationException("Use either --old-keys or --legacy-built-in-keys, not both.");
            }
            else if (oldKeysPath != null)
            {
                oldKeys = KeyFile.ReadOld(oldKeysPath);
            }
            else if (legacy)
            {
                if (configured != null &&
                    !ConfigRotator.SameKeys(configured, LegacyKeys.ConnectionStringKeys) &&
                    !ConfigRotator.SameKeys(configured, newKeys.ConnectionStringKeys))
                {
                    throw new KeyRotationException(
                        "This configuration sets its own key settings, so the built-in keys don't apply. Omit --legacy-built-in-keys.");
                }

                oldKeys = LegacyKeys.ConnectionStringKeys;
            }
            else if (configured == null)
            {
                throw new KeyRotationException(
                    "The configuration doesn't set all three KeyForConnectionString* settings, so the application used its " +
                    "built-in keys. Pass --legacy-built-in-keys (or --old-keys <file>).");
            }
            else
            {
                oldKeys = configured;
            }

            if (ConfigRotator.SameKeys(oldKeys, newKeys.ConnectionStringKeys))
            {
                if (configured != null && !ConfigRotator.SameKeys(configured, oldKeys))
                {
                    throw new KeyRotationException("--old-keys holds the new key material. Pass the keys the configuration uses now.");
                }

                Cryptography.ValidateConfiguration(ConfigRotator.ReadAppSettings(config));
                OrganizationKeyPlan current = null;
                if (database != null)
                {
                    string connection = ConfigRotator.GetDatabaseConnectionString(config, database, oldKeys, oldKeys);
                    current = SqlOrganizationKeyRotator.Check(connection, oldKeys);
                }

                output.WriteLine(configPath + " already uses the new key material and validates; nothing to do.");
                if (current != null)
                {
                    output.WriteLine("  dbo.Organization: {0} already use the new keys, {1} empty", current.AlreadyCurrent, current.Empty);
                }

                return 0;
            }

            string databaseConnection = database == null
                ? null
                : ConfigRotator.GetDatabaseConnectionString(config, database, oldKeys, newKeys.ConnectionStringKeys);
            IList<string> report = ConfigRotator.Rotate(config, oldKeys, newKeys);

            output.WriteLine((apply ? "Rotating " : "Dry run for ") + configPath);
            if (databaseConnection != null)
            {
                OrganizationKeyPlan plan = SqlOrganizationKeyRotator.Rotate(databaseConnection, oldKeys, newKeys.ConnectionStringKeys, apply);
                output.WriteLine("  dbo.Organization: {0} {1}, {2} already use the new keys, {3} empty",
                    plan.Updates.Count, apply ? "re-encrypted and verified" : "to re-encrypt", plan.AlreadyCurrent, plan.Empty);
            }

            foreach (string line in report)
            {
                output.WriteLine("  " + line);
            }

            if (!apply)
            {
                output.WriteLine("Nothing was written. Stop the application pools, then re-run with --apply.");
                return 0;
            }

            string backup = ConfigRotator.Save(config, configPath);
            output.WriteLine("Updated " + configPath + ".");
            output.WriteLine("Backup with the old secrets: " + backup + " - delete it once the site works.");
            return 0;
        }

        private static Dictionary<string, string> ParseOptions(string[] args)
        {
            var flags = new HashSet<string>(StringComparer.Ordinal) { "apply", "legacy-built-in-keys", "admin-key" };
            var valued = new HashSet<string>(StringComparer.Ordinal) { "out", "keys", "config", "new-keys", "old-keys", "database" };
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new KeyRotationException("Unexpected argument '" + args[i] + "'. Run with --help.");
                }

                string name = args[i].Substring(2);
                if (!flags.Contains(name) && !valued.Contains(name))
                {
                    throw new KeyRotationException("Unknown option --" + name + ". Run with --help.");
                }

                if (options.ContainsKey(name))
                {
                    throw new KeyRotationException("--" + name + " is given more than once.");
                }

                if (flags.Contains(name))
                {
                    options[name] = null;
                }
                else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    options[name] = args[++i];
                }
                else
                {
                    throw new KeyRotationException("--" + name + " needs a value.");
                }
            }

            return options;
        }

        private static string Required(Dictionary<string, string> options, string name)
        {
            string value;
            if (!options.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
            {
                throw new KeyRotationException("--" + name + " is required. Run with --help.");
            }

            return value;
        }

        private static int Fail(TextWriter error, string message)
        {
            error.WriteLine("error: " + message);
            return 1;
        }
    }
}
