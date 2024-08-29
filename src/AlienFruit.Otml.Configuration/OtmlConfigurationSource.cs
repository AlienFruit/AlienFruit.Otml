using Microsoft.Extensions.Configuration;
using System;

namespace AlienFruit.Otml.Configuration
{
    public class OtmlConfigurationSource : IConfigurationSource
    {
        public string? OtmlFile { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new OrmlConfigurationProvider(this.OtmlFile ?? throw new InvalidOperationException("OtmlFile is required parameter"));
        }
    }
}
