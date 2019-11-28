using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Readers
{
    internal class StringTextReader : ITextReader
    {
        private readonly string text;

        private const char lf = '\n';
        private const char cr = '\r';
        private static readonly char[] newLineList = { cr, lf };

        private int currentCharIndex;
        private ulong currentLine;
        private int inLinePosition;

        public CurrentCharLocation CurrentLocation => new CurrentCharLocation(this.currentLine, this.inLinePosition);

        public event EventHandler OnNewLine;

        public StringTextReader(string text)
        {
            this.text = text;
            this.currentCharIndex = 0;
            this.currentLine = 1;
            this.inLinePosition = 0;
        }

        public void Dispose()
        {
        }

        public int Read()
        {
            if (this.currentCharIndex == this.text.Length)
                return -1;
            var current = this.text[currentCharIndex++];

            if (current == lf)
            {
                this.inLinePosition = 0;
                this.currentLine++;
                OnNewLine?.Invoke(this, new EventArgs());
            }
            else
                this.inLinePosition++;
            return current;
        }

        public void ToLineEnd()
        {
            int currentChar = 0;
            while ((currentChar = Read()) >= 0)
            {
                var nextChar = this.text[currentCharIndex];
                if (newLineList.Contains((char)currentChar) && (currentCharIndex + 1 == this.text.Length || !newLineList.Contains(nextChar)))
                    break;
            }
        }

        public string ReadLine()
        {
            var result = new List<char>();
            int currentChar = 0;
            while (currentCharIndex < this.text.Length)
            {
                currentChar = this.text[currentCharIndex++];
                if (newLineList.Contains((char)currentChar))
                    break;
                result.Add((char)currentChar);
            }
            return new string(result.ToArray());
        }

        public string PeekToNextLine()
        {
            var result = new List<char>();

            int currentChar = 0;
            int posOffset = 0;
            while (currentCharIndex < this.text.Length)
            {
                currentChar = this.text[currentCharIndex + posOffset++];
                if (newLineList.Contains((char)currentChar))
                    break;
                result.Add((char)currentChar);
            }
            return new string(result.ToArray());
        }
    }
}