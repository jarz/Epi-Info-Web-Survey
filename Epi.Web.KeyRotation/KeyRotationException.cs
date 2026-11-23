using System;

namespace Epi.Web.KeyRotation
{
    /// <summary>
    /// A rotation problem the operator must resolve. Messages name settings and rows but never contain secret values.
    /// </summary>
    [Serializable]
    public class KeyRotationException : Exception
    {
        public KeyRotationException(string message)
            : base(message)
        {
        }
    }
}
