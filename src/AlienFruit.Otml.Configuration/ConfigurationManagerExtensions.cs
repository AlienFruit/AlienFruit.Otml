using Microsoft.Extensions.Configuration;

namespace AlienFruit.Otml.Configuration
{
    public static class ConfigurationManagerExtensions
    {
        public static ConfigurationManager AddOtmlConfigurationFile(this ConfigurationManager manager, string configurationFile)
        {
            manager.Add<OtmlConfigurationSource>(options =>
            {
                options.OtmlFile = configurationFile;
            });
            return manager;
        }
    }
}
