using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AlienFruit.Otml.Readers
{
    internal class StreamTextReader : ITextReader
    {
        private readonly TextReader reader;
        private const char lf = '\n';
        private const char cr = '\r';
        private static readonly char[] newLineList = { cr, lf };

        private readonly Queue<char> peekBuffer = new Queue<char>();

        private ulong currentLine;
        private int inLinePosition;
        private bool disposed = false;

        public event EventHandler OnNewLine;

        public StreamTextReader(Stream stream)
        {
            this.reader = new StreamReader(stream, true);
            this.currentLine = 1;
            this.inLinePosition = 0;
        }

        public StreamTextReader(Stream stream, Encoding encoding, bool leaveOpen)
        {
            this.reader = new StreamReader(stream, encoding, true, 1024, leaveOpen);
        }

        public CurrentCharLocation CurrentLocation => new CurrentCharLocation(this.currentLine, this.inLinePosition);

        public int Read()
        {
            var current = peekBuffer.Count > 0 ? peekBuffer.Dequeue() : reader.Read();
            char c = (char)current;
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

        public string ReadLine()
        {
            var result = new List<char>();
            int currentchar = 0;
            while ((currentchar = peekBuffer.Count > 0 ? peekBuffer.Dequeue() : reader.Read()) >= 0)
            {
                if (newLineList.Contains((char)currentchar))
                    break;
                result.Add((char)currentchar);
            }
            return new string(result.ToArray());
        }

        public string PeekToNextLine()
        {
            var result = new List<char>();
            int currentChar = 0;
            while ((currentChar = reader.Read()) >= 0)
            {
                peekBuffer.Enqueue((char)currentChar);

                if (newLineList.Contains((char)currentChar))
                    break;
                result.Add((char)currentChar);
            }
            return new string(result.ToArray());
        }

        public void ToLineEnd()
        {
            int currentChar = 0;
            while ((currentChar = Read()) >= 0)
                if (newLineList.Contains((char)currentChar) && (reader.Peek() < 0 || !newLineList.Contains((char)reader.Peek())))
                    break;
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

            disposed = true;   
        }
    }
}