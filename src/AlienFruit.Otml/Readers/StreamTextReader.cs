using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlienFruit.Otml.Readers
{
    internal class StreamTextReader : ITextReader
    {
        private readonly TextReader reader;
        // Design decision: OTML is a new format, only '\n' counts as a line break
        // ('\r\n' is also accepted: the trailing '\r' is cut off in ReadLine/PeekToNextLine).
        // A lone '\r' (the classic Mac OS line break) is an ordinary character and is
        // NOT a line break. We do not support backward compatibility with pre-2001 systems.
        private const char lf = '\n';
        private const char cr = '\r';

        private readonly Queue<char> peekBuffer = new Queue<char>();

        private ulong currentLine = 1;
        private int inLinePosition = 0;
        private bool disposed;

        public event EventHandler OnNewLine;

        public StreamTextReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            this.reader = new StreamReader(stream, true);
        }

        public StreamTextReader(Stream stream, Encoding encoding, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            this.reader = new StreamReader(stream, encoding, true, 1024, leaveOpen);
        }

        public SourceLocation CurrentLocation =>
            new SourceLocation(this.currentLine, this.inLinePosition);

        public int Read()
        {
            if (this.disposed)
                return -1;

            int current = this.peekBuffer.Count > 0
                ? this.peekBuffer.Dequeue()
                : this.reader.Read();

            if (current < 0)
                return -1;

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

            // CRLF: cut off the trailing '\r'
            if (result.Length > 0 && result[result.Length - 1] == cr)
                result.Length--;

            return result.ToString();
        }

        public string PeekToNextLine()
        {
            // peek without moving the position: read characters up to and including '\n'
            // into a temporary list, then put them back into peekBuffer.
            if (this.disposed)
                return string.Empty;

            var peeked = new List<char>();
            var result = new StringBuilder();
            int current;
            while ((current = this.peekBuffer.Count > 0
                        ? this.peekBuffer.Dequeue()
                        : this.reader.Read()) >= 0)
            {
                peeked.Add((char)current);
                if (current == lf)
                    break;
                result.Append((char)current);
            }

            foreach (var c in peeked)
                this.peekBuffer.Enqueue(c);

            // CRLF: cut off the trailing '\r'
            if (result.Length > 0 && result[result.Length - 1] == cr)
                result.Length--;

            return result.ToString();
        }

        public void ToLineEnd()
        {
            int currentChar;
            while ((currentChar = Read()) >= 0 && currentChar != lf) { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
                this.reader.Dispose();

            this.disposed = true;
        }
    }
}