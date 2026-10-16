namespace AlienFruit.Otml.Configuration.Abstractions
{
    /// <summary>
    /// Abstraction over a secrets storage (Keyraft, Vault, Key Vault, env-based, etc).
    /// </summary>
    /// <remarks>
    /// A missing secret is NOT an error: implementations return <see langword="null"/> and the
    /// configuration provider moves on to the next registered storage. Implementations MUST throw
    /// only for real failures (transport, authentication, timeout, malformed response), which abort
    /// the whole resolution instead of falling through to the next provider.
    /// An empty string is treated the same as <see langword="null"/> by the consumer.
    /// </remarks>
    public interface ISecretProvider
    {
        /// <summary>
        /// Human-readable storage name used in resolution error messages,
        /// for example <c>keyraft:myapp/prod</c>.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the secret value by its name or <see langword="null"/> when this storage
        /// does not contain the key.
        /// </summary>
        /// <param name="name">Secret name taken from the <c>{{secret:&lt;name&gt;}}</c> marker.</param>
        string? GetSecret(string name);
    }
}
