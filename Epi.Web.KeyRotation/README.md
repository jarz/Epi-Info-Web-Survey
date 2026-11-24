# Epi.Web.KeyRotation

Web Survey encrypts its connection strings, the `AdminKey` setting and the organization keys stored in
`dbo.Organization` with key material from `appSettings`. Each deployment must use its own key material:
the applications refuse to start when the `KeyForConnectionString*` settings are missing, malformed or
publicly known values, and they no longer fall back to built-in values.

This console tool creates deployment key material and moves an existing deployment onto it. It runs on
.NET Framework 4.8 and is built with the solution (`Epi.Web.KeyRotation.exe`).

## Settings

| Setting | Purpose |
| --- | --- |
| `KeyForConnectionStringPassphrase`, `KeyForConnectionStringSalt`, `KeyForConnectionStringVector` | Required. The vector must be exactly 16 ASCII characters. |
| `AdminKey` | Optional. The encrypted admin key GUID used by the publishing service. Leave it empty to disable admin operations. |
| `TokenPassPhrase`, `TokenSaltValue`, `TokenInitVector` | Only needed when `ViewRecordsURL` is used. The application at that URL must decrypt tokens with the same values. |
| `DataServiceSharedKey` | Required by remote WCF callers. Use a unique random 64-character hexadecimal key per deployment, and configure the same value in the service host and every caller. |

## WCF service authorization

The DataService and ManagerService WCF versions reject every SOAP request unless it includes the configured
`DataServiceSharedKey` header over HTTPS. The web client and Survey Manager desktop client add this header;
both require the key to be configured and the endpoint URL to use HTTPS. The desktop client uses normal
certificate validation and does not accept untrusted certificates. The publish-host template disables
metadata publishing and leaves the key empty, so it denies requests until deployment configuration is complete.

Generate a key with a cryptographic random-number generator and keep it out of source control:

```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$dataServiceSharedKey = -join ($bytes | ForEach-Object { $_.ToString('x2') })
$rng.Dispose()
```

Set the resulting value in the protected configuration of the WCF host and each remote client. Do not reuse
the key across deployments. Integrated in-process web mode does not use this SOAP credential. Hosted IIS/WCF
validation is still required before deploying the change.

## New deployment

```powershell
.\Epi.Web.KeyRotation.exe generate --out D:\secure\eiws.keys
'Data Source=...;Initial Catalog=...;Integrated Security=True' | .\Epi.Web.KeyRotation.exe encrypt --keys D:\secure\eiws.keys
.\Epi.Web.KeyRotation.exe encrypt --keys D:\secure\eiws.keys --admin-key
```

Copy the key settings from the key file and the encrypted values into each `Web.config`, store the key file's
values in your secret store and delete the file. Give the `AdminKeyGuid` only to administrators who publish
on behalf of other organizations.

## Existing deployment

1. Back up the database and every `Web.config` that shares it: the survey site, the publishing service and,
   if deployed, the Epi Info Survey API (which uses the same key settings and organization keys).
2. Stop the application pools for those sites.
3. Generate key material once: `.\Epi.Web.KeyRotation.exe generate --out D:\secure\eiws.keys`.
4. Dry run the site whose configuration holds the database connection string, once per database:

   ```powershell
   .\Epi.Web.KeyRotation.exe rotate --config <site>\Web.config --new-keys D:\secure\eiws.keys `
       --legacy-built-in-keys --database EIWSEntities
   ```

   Use `--legacy-built-in-keys` when the configuration has no `KeyForConnectionString*` settings. When it
   has its own settings, omit the option; when the old values are stored elsewhere, pass them in a file with
   `--old-keys` (same `name=value` format as the generated file). The report lists every setting it would
   change and never prints key or connection-string values. Nothing is written without `--apply`.
5. Re-run the same command with `--apply`. The organization keys are re-encrypted in one transaction and
   checked before commit; the configuration is written afterwards, with a backup named
   `Web.<timestamp>.keyrotation-backup.config` next to it.
6. Rotate every other configuration of the deployment with the same key file, without `--database`. Survey
   API configurations set their own key settings, so omit `--legacy-built-in-keys` for them.
7. Start the application pools and check that the sites work. Then delete the backups and the key file.

Organization keys held by users don't change; only their stored encryption does. If the database step
succeeded but the configuration wasn't written, run the same command again: rows that already use the new
keys are recognized and left alone. If a configuration was rotated without `--database`, re-running with
`--database` checks every organization key against the new keys and reports what to do; pass the previous
keys with `--old-keys` (or `--legacy-built-in-keys`) to re-encrypt rows that still use them. The tool
refuses to run when a row or an encrypted connection string can't be decrypted with either key set, when a
section uses `configSource`, or when `--legacy-built-in-keys` is given for a configuration that sets keys of
its own other than the new ones. Plain-text connection strings (for example `ApplicationServices`) are
reported and left unchanged.

`AdminKey` is replaced with the key file's `AdminKeyGuid`. To keep the existing admin key, remove
`AdminKeyGuid` from the key file; a publicly known admin key is never kept. Token settings are replaced only
in configurations that already have them, so rotate the `ViewRecordsURL` application at the same time.
