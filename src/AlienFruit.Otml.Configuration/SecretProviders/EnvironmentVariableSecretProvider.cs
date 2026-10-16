using AlienFruit.Otml.Configuration.Abstractions;
using System;

namespace AlienFruit.Otml.Configuration.SecretProviders
{
    /// <summary>
    /// <see cref="ISecretProvider"/> backed by process environment variables:
    /// <c>{{secret:NAME}}</c> markers resolve to the value of <c>NAME</c> read through
    /// <see cref="Environment.GetEnvironmentVariable(string)"/>.
    /// </summary>
    /// <remarks>
    /// A missing variable is not an error: <see cref="GetSecret"/> returns <see langword="null"/>
    /// and resolution moves on to the next registered storage. An existing but empty variable
    /// is treated as a miss by the consumer as well.
    /// Values are read at resolution time (lazily), so rotating a variable takes effect on
    /// configuration reload; <see cref="SecretProviderRegistry"/> and DI registration themselves
    /// are independent of the current environment. Names are read with the OS semantics of
    /// <see cref="Environment.GetEnvironmentVariable(string)"/>: case-insensitive on Windows,
    /// case-sensitive on Linux.
    /// </remarks>
    public class EnvironmentVariableSecretProvider : ISecretProvider
    {
        /// <inheritdoc />
        public string Name => "environment";

        /// <inheritdoc />
        public string? GetSecret(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return Environment.GetEnvironmentVariable(name);
        }
    }
}
