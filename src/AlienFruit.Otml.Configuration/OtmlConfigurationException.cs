using System;

namespace AlienFruit.Otml.Configuration
{
    /// <summary>
    /// Thrown by <see cref="OtmlConfigurationProvider.Load()"/> when an OTML document is
    /// syntactically valid but cannot be flattened into configuration keys: a property
    /// name (or array index) repeats — the second declaration would collide with the first —
    /// a property carries no value, or an object carries values that have no configuration
    /// key to be stored under. Unlike the parse errors raised by the OTML parser,
    /// this failure happens while building the key/value store, so the exception belongs
    /// to the configuration library. The message follows the <c>OtmlParseException</c>
    /// style and names the file, line and position of the offending node.
    /// </summary>
    public class OtmlConfigurationException : Exception
    {
        public OtmlConfigurationException(string otmlFile, ulong? line, int? position, string message)
            : base(FormatMessage(otmlFile, line, position, message))
        {
            this.OtmlFile = otmlFile;
            this.Line = line;
            this.Position = position;
        }

        /// <summary>
        /// Full path of the OTML file the provider was loading.
        /// </summary>
        public string OtmlFile { get; }

        /// <summary>
        /// 1-based line of the offending node, or <see langword="null"/> when the position is unknown.
        /// </summary>
        public ulong? Line { get; }

        /// <summary>
        /// 0-based in-line position of the offending node, or <see langword="null"/> when the position is unknown.
        /// </summary>
        public int? Position { get; }

        private static string FormatMessage(string otmlFile, ulong? line, int? position, string message)
            => line is null || position is null
                ? $"{message}, in file: {otmlFile}"
                : $"{message}, in file: {otmlFile}, line: {line}, position: {position}";
    }
}
