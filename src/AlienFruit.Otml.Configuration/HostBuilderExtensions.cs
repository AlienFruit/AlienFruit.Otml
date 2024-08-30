using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AlienFruit.Otml.Configuration
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddOtmlConfigurationFile(this IHostBuilder builder, string configurationFile)
        {
            return builder.ConfigureAppConfiguration((x, y) => y.AddConfiguration(CreateConfiguration(configurationFile)));
        }

        static IConfigurationRoot CreateConfiguration(string file)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.Sources.Add(
                new OtmlConfigurationSource
                {
                    OtmlFile = file
                });
            return configBuilder.Build();
        }
    }
}
