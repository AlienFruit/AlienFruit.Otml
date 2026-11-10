using System;
using System.Text;

namespace AlienFruit.Otml.Readers
{
    internal class StringTextReader : ITextReader
    {
        private readonly string text;
        private const char lf = '\n';
        private const char cr = '\r';

        // Design decision: OTML is a new format, only '\n' counts as a line break
        // ('\r\n' is also accepted: the trailing '\r' is cut off in ReadLine/PeekToNextLine).
        // A lone '\r' (the classic Mac OS line break) is an ordinary character and is
        // NOT a line break. We do not support backward compatibility with pre-2001 systems.
        private static bool IsNewLine(char c) => c == lf;

        private int currentCharIndex;
        private ulong currentLine;
        private int inLinePosition;
        private bool disposed;

        public SourceLocation CurrentLocation =>
            new SourceLocation(this.currentLine, this.inLinePosition);

        public event EventHandler OnNewLine;

        public StringTextReader(string text)
        {
            var value = text ?? string.Empty;

            // A BOM (U+FEFF) at the start of the text is not content: StreamTextReader
            // (via StreamReader) swallows it, so we do the same here
            // to ensure the same document reads identically from a string and from a stream.
            if (value.Length > 0 && value[0] == '\uFEFF')
                value = value.Substring(1);

            this.text = value;
            this.currentCharIndex = 0;
            this.currentLine = 1;
            this.inLinePosition = 0;
        }

        public void Dispose() => this.disposed = true;

        public int Read()
        {
            if (this.disposed)
                return -1;

            if (this.currentCharIndex == this.text.Length)
                return -1;

            var current = this.text[this.currentCharIndex++];

            if (current == lf)
            {
                this.inLinePosition = 0;
                this.currentLine++;
                OnNewLine?.Invoke(this, EventArgs.Empty);
            }
            else
                this.inLinePosition++;

            return current;
        }

        public void ToLineEnd()
        {
            int currentChar;
            while ((currentChar = Read()) >= 0 && currentChar != lf) { }
        }

        public string ReadLine()
        {
            var firstChar = Read();
            if (firstChar < 0)
                return null; // EOF: there is no next line (System.Text.TextReader contract)

            var result = new StringBuilder();
            int currentChar = firstChar;
            while (currentChar >= 0 && currentChar != lf)
            {
                result.Append((char)currentChar);
                currentChar = Read();
            }

            // For CRLF: cut off the trailing '\r'
            if (result.Length > 0 && result[result.Length - 1] == cr)
                result.Length--;

            return result.ToString();
        }

        public string PeekToNextLine()
        {
            if (this.disposed)
                return string.Empty;

            var result = new StringBuilder();
            int i = this.currentCharIndex;
            while (i < this.text.Length && this.text[i] != lf)
            {
                result.Append(this.text[i]);
                i++;
            }
            // For CRLF: cut off the trailing '\r'
            if (result.Length > 0 && result[result.Length - 1] == cr)
                result.Length--;
            return result.ToString();
        }
    }
}