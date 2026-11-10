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
        private Dictionary<string, string?> resolvedCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        public OtmlConfigurationProvider(string otmlFile, ISecretProvider[] secrets)
        {
            this.otmlFile = otmlFile ?? throw new ArgumentNullException(nameof(otmlFile));
            this.secretProviders = secrets ?? throw new ArgumentNullException(nameof(secrets));
            // Match the Microsoft.Extensions.Configuration convention (JSON/XML/INI
            // providers): keys are looked up case-insensitively.
            this.Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
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
                // Root-level objects become sections; root-level properties become
                // root keys, like JSON/INI root scalars (e.g. AllowedHosts).
                if (node.Type == NodeType.Object)
                    ProcessObject(string.Empty, node);
                else if (node.Type == NodeType.Property)
                    ProcessProperty(string.Empty, node);
            }
        }

        private void ProcessObject(string parrentKey, OtmlNode node)
        {
            var key = GetKey(parrentKey, node.Name);

            // OTML allows values on an object (inline `@s : v` or on the new lines),
            // but a configuration section cannot carry a value: the key `s` is already
            // taken by the section itself, so there is nowhere to store them. Fail the
            // load instead of silently dropping the data.
            if (node.Children.Any(x => x.Type == NodeType.Value))
                throw new OtmlConfigurationException(this.otmlFile, node.Location?.Line, node.Location?.Position,
                    $"Object cannot have values. Object name:{node.Name}");

            foreach (var item in node.Children)
            {
                if (item.Type == NodeType.Object)
                {
                    ProcessObject(key, item);
                    continue;
                }

                if (item.Type == NodeType.Property)
                    ProcessProperty(key, item);
            }
        }

        /// <summary>
        /// Flattens one property node into configuration keys under <paramref name="parrentKey"/>:
        /// a scalar becomes a single key, an array becomes indexed keys. A root-level
        /// property uses an empty parent key and therefore maps straight onto a root key.
        /// </summary>
        private void ProcessProperty(string parrentKey, OtmlNode property)
        {
            var values = GetValues(property);

            if (property.IsArray)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    var itemKey = GetKey(parrentKey, property.Name) + ':' + i;
                    AddValue(itemKey, values[i], property);
                }
            }
            else
            {
                var itemKey = GetKey(parrentKey, property.Name);
                AddValue(itemKey, values[0], property);
            }
        }

        /// <summary>
        /// Stores a single configuration value, turning the raw
        /// <see cref="ArgumentException"/> of <c>Dictionary.Add</c> into a domain
        /// <see cref="OtmlConfigurationException"/> that names the file, line and
        /// position of the offending property node (the OTML parser error style).
        /// A duplicate key means the same property name (or the same array index,
        /// including a case-only difference: keys are case-insensitive) is declared twice.
        /// </summary>
        private void AddValue(string key, string value, OtmlNode node)
        {
            if (this.Data.ContainsKey(key))
                throw new OtmlConfigurationException(this.otmlFile, node.Location?.Line, node.Location?.Position,
                    $"Duplicate property name '{node.Name}': configuration key '{key}' is already defined.");

            this.Data.Add(key, value);
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

        private string[] GetValues(OtmlNode node)
        {
            var values = node.Children.Where(x => x.Type == NodeType.Value).ToArray();
            if (values.Length == 0)
                throw new OtmlConfigurationException(this.otmlFile, node.Location?.Line, node.Location?.Position,
                    $"Property cannot be empty. Property name:{node.Name}");

            return values.Select(x => x.Value).ToArray();
        }

        /// <summary>
        /// Replaces every <c>{{secret:&lt;name&gt;}}</c> marker in the value with the first
        /// non-empty secret returned by the registered <see cref="ISecretProvider"/> storages,
        /// tried in registration order. A storage that does not contain the key returns
        /// <see langword="null"/> and resolution moves on to the next one. Only when no storage
        /// returned a value the resolution fails with <see cref="SecretNotFoundException"/>.
        /// Markers with unknown tags and values without markers pass through unchanged.
        /// Resolution is single-pass by design: a value returned by a storage is opaque data,
        /// it is never re-scanned for markers, so nested markers are not supported —
        /// a storage value that looks like <c>{{secret:...}}</c> is returned verbatim.
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
