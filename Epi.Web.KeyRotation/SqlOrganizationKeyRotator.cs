using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Epi.Web.Common.Security;

namespace Epi.Web.KeyRotation
{
    /// <summary>
    /// Re-encrypts dbo.Organization.OrganizationKey in one serializable transaction holding an exclusive table lock.
    /// </summary>
    public static class SqlOrganizationKeyRotator
    {
        private const string SelectRows =
            "SELECT OrganizationId, OrganizationKey FROM dbo.Organization WITH (TABLOCKX, HOLDLOCK) ORDER BY OrganizationId";

        private const string UpdateRow =
            "UPDATE dbo.Organization SET OrganizationKey = @newValue WHERE OrganizationId = @organizationId AND OrganizationKey = @oldValue";

        /// <summary>
        /// Plans the rotation and, when <paramref name="apply"/> is true, updates and verifies every row before committing.
        /// A dry run rolls back without writing.
        /// </summary>
        public static OrganizationKeyPlan Rotate(string connectionString, KeyMaterial oldKeys, KeyMaterial newKeys, bool apply)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    OrganizationKeyPlan plan = OrganizationKeyPlan.Create(ReadRows(connection, transaction), oldKeys, newKeys);
                    if (!apply)
                    {
                        transaction.Rollback();
                        return plan;
                    }

                    foreach (OrganizationKeyUpdate update in plan.Updates)
                    {
                        using (SqlCommand command = new SqlCommand(UpdateRow, connection, transaction))
                        {
                            command.Parameters.Add("@newValue", SqlDbType.NVarChar, 4000).Value = update.NewValue;
                            command.Parameters.Add("@organizationId", SqlDbType.Int).Value = update.OrganizationId;
                            command.Parameters.Add("@oldValue", SqlDbType.NVarChar, 4000).Value = update.OldValue;
                            if (command.ExecuteNonQuery() != 1)
                            {
                                throw new KeyRotationException("OrganizationId " + update.OrganizationId + " changed during rotation. Nothing was changed.");
                            }
                        }
                    }

                    plan.Verify(ReadRows(connection, transaction), newKeys);
                    transaction.Commit();
                    return plan;
                }
            }
        }

        private static List<OrganizationKeyRow> ReadRows(SqlConnection connection, SqlTransaction transaction)
        {
            var rows = new List<OrganizationKeyRow>();
            using (var command = new SqlCommand(SelectRows, connection, transaction))
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new OrganizationKeyRow(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
                }
            }

            return rows;
        }
    }
}
