using AlienFruit.Otml.Configuration.Abstractions;
using AlienFruit.Otml.Factories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlienFruit.Otml.Configuration
{
    public class OtmlConfigurationProvider : ConfigurationProvider
    {
        private readonly string otmlFile;
        private readonly ISecretProvider[] secretProviders;
        private readonly object syncRoot = new object();
        private Dictionary<string, string?> resolvedCache = new Dictionary<string, string?>();

        public OtmlConfigurationProvider(string otmlFile, ISecretProvider[] secrets)
        {
            this.otmlFile = otmlFile ?? throw new ArgumentNullException(nameof(otmlFile));
            this.secretProviders = secrets ?? throw new ArgumentNullException(nameof(secrets));
            this.Data = new Dictionary<string, string?>();
        }

        public override void Load()
        {
            var otmlFactory = new OtmlParserFactory();
            using var stream = File.OpenRead(this.otmlFile);
            using var parser = otmlFactory.GetParser(stream);
            var dom = parser.Parse();

            lock (this.syncRoot)
            {
                this.Data.Clear();
                this.resolvedCache.Clear();
            }

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

                    if (item.IsArray)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            var itemKey = GetKey(key, item.Name) + ':' + i;
                            Data.Add(itemKey, values[i]);
                        }
                    }
                    else
                    {
                        var itemKey = GetKey(key, item.Name);
                        Data.Add(itemKey, values[0]);
                    }
                }
            }
        }

        public override bool TryGet(string key, out string? value)
        {
            if (!base.TryGet(key, out value))
                return false;

            lock (this.syncRoot)
            {
                if (this.resolvedCache.TryGetValue(key, out var cached))
                {
                    value = cached;
                    return true;
                }
            }

            value = ResolveSecrets(value, key);

            lock (this.syncRoot)
            {
                this.resolvedCache[key] = value;
            }

            return true;
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

        /// <summary>
        /// Replaces every <c>{{secret:&lt;name&gt;}}</c> marker in the value with the first
        /// non-empty secret returned by the registered <see cref="ISecretProvider"/> storages,
        /// tried in registration order. A storage that does not contain the key returns
        /// <see langword="null"/> and resolution moves on to the next one. Only when no storage
        /// returned a value the resolution fails with <see cref="SecretNotFoundException"/>.
        /// Markers with unknown tags and values without markers pass through unchanged.
        /// Resolution happens on the first <see cref="TryGet"/> access to the value, not at load time.
        /// </summary>
        private string? ResolveSecrets(string? value, string configurationKey)
        {
            if (value is null || !SecretMarkerRegex.IsMatch(value))
                return value;

            return SecretMarkerRegex.Replace(value, match =>
            {
                var secretName = match.Groups[1].Value.Trim();
                if (secretName.Length == 0)
                    throw new ArgumentException(
                        $"The value of '{configurationKey}' contains an empty {{{{secret:...}}}} marker.");

                foreach (var provider in this.secretProviders)
                {
                    var secret = provider.GetSecret(secretName);
                    if (!string.IsNullOrEmpty(secret))
                        return secret;
                }

                var searched = this.secretProviders.Length == 0
                    ? $"no {nameof(ISecretProvider)} is registered"
                    : "searched storages: " + string.Join(", ", this.secretProviders.Select(p => p.Name));

                throw new SecretNotFoundException(secretName,
                    $"Secret '{secretName}' referenced by the configuration key '{configurationKey}' was not found ({searched}).");
            });
        }

        private static readonly Regex SecretMarkerRegex =
            new Regex(@"\{\{\s*secret\s*:([^{}]+?)\s*\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
