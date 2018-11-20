using AlienFruit.Otml.Parsers;
using AlienFruit.Otml.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AlienFruit.Otml.Factories
{
    public class OtmlParserFactory : IParserFactory
    {
        private const string declarationPrefix = "@@";
        private const char splitChar = ':';
        private const string commentChar = "#";
        private const string versionDeclaration = "version";
        private const string encodingDeclatation = "encoding";

        private readonly IDictionary<Version, Func<ITextReader, IParser>> parsersMap;

        private readonly Encoding defaultEncoding;

        public OtmlParserFactory(Encoding defaultEnconding)
        {
            this.defaultEncoding = defaultEnconding;
            this.parsersMap = new Dictionary<Version, Func<ITextReader, IParser>>
            {
                { new Version("1.0"), x => new OtmlParser(x) }
            };
        }

        public IParser GetParser(string otmlText) => GetParser(new StringTextReader(otmlText));

        public IParser GetParser(Stream stream, bool leaveOpen)
        {
            using (var reader = new StreamTextReader(stream, this.defaultEncoding, true))
                return GetParser(reader);
        }

        private IParser GetParser(ITextReader reader)
        {
            var declaration = ParseDeclaration(reader);
            var maxVersion = parsersMap.Keys.Max();

            var encoding = declaration.Encodind ?? this.defaultEncoding;

            if (declaration.Version is null)
                return parsersMap[maxVersion].Invoke(reader);

            if (!parsersMap.TryGetValue(declaration.Version, out var parserCreator))
                throw new InvalidOperationException($"A parser for version {declaration.Version} was not found. Last available version is {maxVersion}");

            return parserCreator.Invoke(reader);
        }

        internal static OtmlDeclaration ParseDeclaration(ITextReader reader)
        {
            var result = new OtmlDeclaration();

            uint lineNum = 1;

            string line = string.Empty;
            while ((line = reader.PeekToNextLine()).StartsWith(declarationPrefix))
            {
                reader.ToLineEnd();

                var splittedLine = line.Split(splitChar);

                if (splittedLine.Length != 2)
                    throw new ArgumentException($"Invalid  OTML declaration in line {lineNum}");

                var name = splittedLine[0].Substring(declarationPrefix.Length).ToLowerInvariant().TrimEnd();
                var value = splittedLine[1].Trim();

                switch (name)
                {
                    case versionDeclaration:
                        if (!Version.TryParse(value, out var resultVersion))
                            throw new ArgumentException($"Cannot parse version in OTML declaration in line {lineNum}");
                        result.Version = resultVersion;
                        break;

                    case encodingDeclatation:
                        try
                        {
                            result.Encodind = Encoding.GetEncoding(value);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"Cannot parse encoding in OTML declaration in line {lineNum}", ex);
                        }
                        break;

                    default: throw new ArgumentException($"Unknown  OTML declaration in line {lineNum}");
                }

                lineNum++;
            }

            return result;
        }

        internal class OtmlDeclaration
        {
            public Version Version { get; set; }
            public Encoding Encodind { get; set; }
        }
    }
}