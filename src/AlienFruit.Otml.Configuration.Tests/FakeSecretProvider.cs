using AlienFruit.Otml.Configuration.Abstractions;
using System;
using System.Collections.Generic;

namespace AlienFruit.Otml.Configuration.Tests
{
    /// <summary>
    /// Contract-compliant fake: returns <see langword="null"/> for unknown names
    /// (the absence of a secret is not an error).
    /// </summary>
    public class FakeSecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> secrets;

        public FakeSecretProvider(IDictionary<string, string> secrets, string name = "fake")
        {
            this.secrets = new Dictionary<string, string>(secrets, StringComparer.Ordinal);
            Name = name;
        }

        public string Name { get; }

        public List<string> RequestedNames { get; } = new List<string>();

        public string? GetSecret(string name)
        {
            RequestedNames.Add(name);

            return this.secrets.TryGetValue(name, out var value) ? value : null;
        }
    }
}
