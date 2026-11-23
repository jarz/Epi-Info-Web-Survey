using Epi.Web.Common.Security;

namespace Epi.Web.KeyRotation
{
    /// <summary>
    /// The built-in key material that Epi.Web.Common used before deployment-specific keys were required.
    /// These values are public in the Epi Info source history, so the application now rejects them; the
    /// rotation tool uses them only to decrypt existing data (--legacy-built-in-keys).
    /// </summary>
    public static class LegacyKeys
    {
        public static KeyMaterial ConnectionStringKeys
        {
            get { return new KeyMaterial("80787d6053694493be171dd712e51c61", "476ba16073764022bc7f262c6d67ebef", "0f8f*d5bd&cb4~9f"); }
        }
    }
}