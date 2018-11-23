using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlienFruit.Otml.Factories
{
    public class OtmlUnparserFactory : IUnparserFactory
    {
        private readonly IDictionary<Version, Func<Encoding, IUnparser>> unparsersMap;
        private readonly Encoding defaultEncoding;

        public OtmlUnparserFactory(Encoding defaultEncoding = null)
        {
            this.defaultEncoding = defaultEncoding ?? Encoding.UTF8;
            this.unparsersMap = new Dictionary<Version, Func<Encoding, IUnparser>>
            {
                { new Version("1.0"), x => new Domain.Version1v0.OtmlUnparser(x)}
            };
        }

        public IUnparser GetDefaultUnparser(Encoding encoding = null)
        {
            var maxVersion = unparsersMap.Keys.Max();
            return unparsersMap[maxVersion].Invoke(encoding ?? this.defaultEncoding);
        }

        public IUnparser GetUnparser(Version version, Encoding encoding = null)
        {
            var maxVersion = unparsersMap.Keys.Max();
            if (!unparsersMap.TryGetValue(version, out var parserCreator))
                throw new InvalidOperationException($"A parser for version {version} was not found. Last available version is {maxVersion}");
            return parserCreator.Invoke(encoding ?? this.defaultEncoding);
        }
    }
}