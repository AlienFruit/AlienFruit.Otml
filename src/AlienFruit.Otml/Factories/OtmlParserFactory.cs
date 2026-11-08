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

        public OtmlParserFactory(Encoding defaultEnconding = null)
        {
            this.defaultEncoding = defaultEnconding ?? Encoding.UTF8;
            this.parsersMap = new Dictionary<Version, Func<ITextReader, IParser>>
            {
                { new Version("1.0"), x => new Domain.Version1v0.OtmlParser(x) }
            };
        }

        public IParser GetParser(string otmlText) => GetParser(new StringTextReader(otmlText));

        public IParser GetParser(Stream stream, bool leaveOpen = false)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            Stream source = stream;
            long? startPosition = null;
            if (stream.CanSeek)
                startPosition = stream.Position;
            else
            {
                // non-seekable stream: sniffing would consume the beginning, so work with a copy;
                // when leaveOpen == false, ownership of the original is handed to the parser — dispose it
                var copy = new MemoryStream();
                stream.CopyTo(copy);
                copy.Position = 0;
                if (!leaveOpen)
                    stream.Dispose();
                source = copy;
            }

            // the @@encoding value can only be learned by reading the declaration,
            // and the declaration cannot be read without an encoding: first try the base
            // encoding; if it misses, guess from the bytes (StreamReader detects the BOM itself)
            var encoding = SniffStreamEncoding(source, this.defaultEncoding);
            source.Position = startPosition ?? 0;

            return GetParser(new StreamTextReader(source, encoding, leaveOpen));
        }

        private static readonly Encoding[] sniffCandidates = { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode };

        private static Encoding SniffStreamEncoding(Stream stream, Encoding defaultEncoding)
        {
            var buffer = new byte[4096];
            var read = 0;
            int count;
            while (read < buffer.Length && (count = stream.Read(buffer, read, buffer.Length - read)) > 0)
                read += count;

            if (read == 0)
                return defaultEncoding;

            // BOM present — detectEncodingFromByteOrderMarks in StreamReader will handle it
            if (buffer[0] == 0xEF && read >= 3 && buffer[1] == 0xBB && buffer[2] == 0xBF ||
                read >= 2 && (buffer[0] == 0xFF && buffer[1] == 0xFE || buffer[0] == 0xFE && buffer[1] == 0xFF))
                return defaultEncoding;

            var defaultText = defaultEncoding.GetString(buffer, 0, read);

            // base encoding guessed correctly: @@encoding inside the declaration takes priority
            if (LooksLikeDeclaration(defaultText))
                return TryReadDeclaredEncoding(defaultText) ?? defaultEncoding;

            foreach (var candidate in sniffCandidates)
            {
                if (string.Equals(candidate.WebName, defaultEncoding.WebName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var text = candidate.GetString(buffer, 0, read);
                if (!LooksLikeDeclaration(text))
                    continue;

                return TryReadDeclaredEncoding(text) ?? candidate;
            }

            return defaultEncoding;
        }

        // the declaration must be the first line; with a wrong base encoding
        // "@@version" turns into garbage or '\0'-separated characters
        private static bool LooksLikeDeclaration(string text) =>
            text.StartsWith(declarationPrefix + versionDeclaration, StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith(declarationPrefix + encodingDeclatation, StringComparison.OrdinalIgnoreCase);

        private static Encoding TryReadDeclaredEncoding(string text)
        {
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (!line.StartsWith(declarationPrefix))
                    break; // declarations only appear at the beginning of the document

                var parts = line.Split(splitChar);
                if (parts.Length != 2)
                    continue;

                var name = parts[0].Substring(declarationPrefix.Length).ToLowerInvariant().TrimEnd();
                if (name != encodingDeclatation)
                    continue;

                try
                {
                    return Encoding.GetEncoding(parts[1].Trim());
                }
                catch (ArgumentException)
                {
                    // invalid encoding name — ParseDeclaration will deal with it
                    return null;
                }
            }

            return null;
        }

        private IParser GetParser(ITextReader reader)
        {
            var declaration = ParseDeclaration(reader);
            var maxVersion = parsersMap.Keys.Max();

            if (declaration.Version is null)
                return parsersMap[maxVersion].Invoke(reader);

            if (!parsersMap.TryGetValue(declaration.Version, out var parserCreator))
                throw new InvalidOperationException($"A parser for version {declaration.Version} was not found. Last available version is {maxVersion}");

            return parserCreator.Invoke(reader);
        }

        // "1.0.0" and "1.0.0.0" are the same format version as "1.0": Version("1.0.0") != Version("1.0")
        // due to trailing zeros, so drop the zero components at the tail.
        internal static Version NormalizeVersion(Version version)
        {
            var parts = new List<int> { version.Major, version.Minor };
            if (version.Build >= 0)
                parts.Add(version.Build);
            if (version.Revision >= 0)
                parts.Add(version.Revision);
            while (parts.Count > 2 && parts[parts.Count - 1] == 0)
                parts.RemoveAt(parts.Count - 1);

            if (parts.Count > 3)
                return new Version(parts[0], parts[1], parts[2], parts[3]);
            if (parts.Count == 3)
                return new Version(parts[0], parts[1], parts[2]);
            return new Version(parts[0], parts[1]);
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
                        result.Version = NormalizeVersion(resultVersion);
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