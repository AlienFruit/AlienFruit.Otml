using AlienFruit.Otml.Factories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlienFruit.Otml.Configuration
{
    public class OrmlConfigurationProvider : ConfigurationProvider
    {
        private readonly string otmlFile;

        public OrmlConfigurationProvider(string otmlFile)
        {
            this.otmlFile = otmlFile;
            this.Data = new Dictionary<string, string?>();
        }

        public override void Load()
        {
            var otmlFactory = new OtmlParserFactory();
            var stream = File.OpenRead(this.otmlFile);
            var parser = otmlFactory.GetParser(stream);
            var dom = parser.Parse();

            foreach (var node in dom)
            {
                ProcessObject(string.Empty, node);
            }
        }

        private void ProcessObject(string parrentKey, OtmlNode node)
        {
            var key = GetKey(parrentKey, node.Name);

            foreach (var item in node.Children)
            {
                if (item.Type == NodeType.Object)
                {
                    ProcessObject(key, item);
                    continue;
                }

                if (item.Type == NodeType.Property)
                {
                    var values = GetValues(item);

                    if (values.Length > 1)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            Data.Add(GetKey(key, item.Name) + ':' +i, values[i]);
                        }
                    }
                    else
                    {
                        Data.Add(GetKey(key, item.Name), values[0]);
                    }
                }
            }
        }

        private static string GetKey(string parrentKey, string currentName)
            => string.IsNullOrWhiteSpace(parrentKey) ? currentName : $"{parrentKey}:{currentName}";

        private static string[] GetValues(OtmlNode node)
        {
            var values = node.Children.Where(x => x.Type == NodeType.Value).ToArray();
            if (values.Length == 0)
                throw new ArgumentException($"Property cannot be empty. Property name:{node.Name}");

            return values.Select(x => x.Value).ToArray();
        }
    }
}
