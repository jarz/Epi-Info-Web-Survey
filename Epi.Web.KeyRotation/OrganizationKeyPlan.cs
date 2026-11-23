using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Epi.Web.Common.Security;

namespace Epi.Web.KeyRotation
{
    public sealed class OrganizationKeyRow
    {
        public OrganizationKeyRow(int organizationId, string organizationKey)
        {
            OrganizationId = organizationId;
            OrganizationKey = organizationKey;
        }

        public int OrganizationId { get; }

        public string OrganizationKey { get; }
    }

    public sealed class OrganizationKeyUpdate
    {
        internal OrganizationKeyUpdate(int organizationId, string oldValue, string newValue, string plainText)
        {
            OrganizationId = organizationId;
            OldValue = oldValue;
            NewValue = newValue;
            PlainText = plainText;
        }

        public int OrganizationId { get; }

        public string OldValue { get; }

        public string NewValue { get; }

        internal string PlainText { get; }
    }

    /// <summary>
    /// Decides how to re-encrypt dbo.Organization.OrganizationKey values. Encryption is deterministic and the
    /// applications look organizations up by encrypted key, so each value keeps its exact plaintext.
    /// </summary>
    public sealed class OrganizationKeyPlan
    {
        private OrganizationKeyPlan(IList<OrganizationKeyUpdate> updates, int alreadyCurrent, int empty)
        {
            Updates = updates;
            AlreadyCurrent = alreadyCurrent;
            Empty = empty;
        }

        public IList<OrganizationKeyUpdate> Updates { get; }

        public int AlreadyCurrent { get; }

        public int Empty { get; }

        /// <summary>
        /// Plans the updates; throws without changing anything if any row can't be decrypted with either key set.
        /// </summary>
        public static OrganizationKeyPlan Create(IEnumerable<OrganizationKeyRow> rows, KeyMaterial oldKeys, KeyMaterial newKeys)
        {
            var updates = new List<OrganizationKeyUpdate>();
            int alreadyCurrent = 0;
            int empty = 0;
            foreach (OrganizationKeyRow row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.OrganizationKey))
                {
                    empty++;
                    continue;
                }

                string plainText;
                if (TryDecryptGuidText(row.OrganizationKey, oldKeys, out plainText))
                {
                    updates.Add(new OrganizationKeyUpdate(row.OrganizationId, row.OrganizationKey, Cryptography.Encrypt(plainText, newKeys), plainText));
                }
                else if (TryDecryptGuidText(row.OrganizationKey, newKeys, out plainText))
                {
                    alreadyCurrent++;
                }
                else
                {
                    throw new KeyRotationException(
                        "OrganizationId " + row.OrganizationId + ": OrganizationKey can't be decrypted with the old or new keys. Nothing was changed.");
                }
            }

            return new OrganizationKeyPlan(updates, alreadyCurrent, empty);
        }

        /// <summary>
        /// Confirms every row already decrypts with <paramref name="keys"/>; throws if any row doesn't.
        /// </summary>
        public static OrganizationKeyPlan CheckCurrent(IEnumerable<OrganizationKeyRow> rows, KeyMaterial keys)
        {
            int current = 0;
            int empty = 0;
            foreach (OrganizationKeyRow row in rows)
            {
                string plainText;
                if (string.IsNullOrWhiteSpace(row.OrganizationKey))
                {
                    empty++;
                }
                else if (TryDecryptGuidText(row.OrganizationKey, keys, out plainText))
                {
                    current++;
                }
                else
                {
                    throw new KeyRotationException(
                        "OrganizationId " + row.OrganizationId + ": OrganizationKey doesn't decrypt with the new keys. " +
                        "If the database wasn't rotated yet, pass the previous keys with --old-keys <file> " +
                        "(or --legacy-built-in-keys). Nothing was changed.");
                }
            }

            return new OrganizationKeyPlan(new List<OrganizationKeyUpdate>(), current, empty);
        }

        /// <summary>
        /// Confirms every updated row now decrypts with the new keys to its original plaintext.
        /// </summary>
        public void Verify(IEnumerable<OrganizationKeyRow> rowsAfterUpdate, KeyMaterial newKeys)
        {
            Dictionary<int, string> current = rowsAfterUpdate.ToDictionary(r => r.OrganizationId, r => r.OrganizationKey);
            foreach (OrganizationKeyUpdate update in Updates)
            {
                string value;
                string plainText;
                if (!current.TryGetValue(update.OrganizationId, out value) ||
                    !TryDecryptGuidText(value, newKeys, out plainText) ||
                    !string.Equals(plainText, update.PlainText, StringComparison.Ordinal))
                {
                    throw new KeyRotationException("OrganizationId " + update.OrganizationId + " didn't verify after re-encryption.");
                }
            }
        }

        internal static bool TryDecryptGuidText(string cipherText, KeyMaterial keys, out string plainText)
        {
            plainText = null;
            try
            {
                string candidate = Cryptography.Decrypt(cipherText.Trim(), keys);
                Guid ignored;
                if (Guid.TryParse(candidate, out ignored))
                {
                    plainText = candidate;
                    return true;
                }
            }
            catch (FormatException)
            {
            }
            catch (CryptographicException)
            {
            }

            return false;
        }
    }
}
