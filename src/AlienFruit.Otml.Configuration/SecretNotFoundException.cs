using System;

namespace AlienFruit.Otml.Configuration
{
    /// <summary>
    /// Thrown when a referenced secret does not exist in the secret storage
    /// or when no configured value can be resolved for it.
    /// </summary>
    public class SecretNotFoundException : Exception
    {
        public SecretNotFoundException(string secretName)
            : this(secretName, $"Secret '{secretName}' was not found in the secrets storage.")
        {
        }

        public SecretNotFoundException(string secretName, string message)
            : base(message)
        {
            this.SecretName = secretName;
        }

        public SecretNotFoundException(string secretName, string message, Exception innerException)
            : base(message, innerException)
        {
            this.SecretName = secretName;
        }

        /// <summary>
        /// Name of the missing secret (the argument of the <c>{{secret:&lt;name&gt;}}</c> marker).
        /// </summary>
        public string SecretName { get; }
    }
}
