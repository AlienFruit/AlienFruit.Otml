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
                    Data.Add(GetKey(key, item.Name), GetValue(item));
                }
            }
        }

        private static string GetKey(string parrentKey, string currentName)
            => string.IsNullOrWhiteSpace(parrentKey) ? currentName : $"{parrentKey}:{currentName}";

        private static string GetValue(OtmlNode node)
        {
            var values = node.Children.Where(x => x.Type == NodeType.Value).ToArray();
            if (values.Length > 1)
                throw new ArgumentException($"Property cannot has more then one value. Property name:{node.Name}");

            if (values.Length == 0)
                throw new ArgumentException($"Property cannot be empty. Property name:{node.Name}");

            return values.First().Value;
        }
    }
}
