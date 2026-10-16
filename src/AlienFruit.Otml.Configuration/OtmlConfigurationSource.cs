using AlienFruit.Otml.Configuration.Abstractions;
using Microsoft.Extensions.Configuration;
using System;

namespace AlienFruit.Otml.Configuration
{
    public class OtmlConfigurationSource : IConfigurationSource
    {
        public string OtmlFile { get; set; }

        public ISecretProvider[] SecretProviders { get; set; } = Array.Empty<ISecretProvider>();

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(this.OtmlFile))
                throw new InvalidOperationException("OtmlFile is required parameter");

            return new OtmlConfigurationProvider(this.OtmlFile, this.SecretProviders);
        }
    }
}
