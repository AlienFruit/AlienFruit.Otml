using AlienFruit.Otml.Configuration.Abstractions;
using System;

namespace AlienFruit.Otml.Configuration
{
    public class OtmlConfigurationOptions
    {
        public string OtmlFile { get; set; }
        public ISecretProvider[] CustomSecretProviders { get; set; } = Array.Empty<ISecretProvider>();
    }
}
