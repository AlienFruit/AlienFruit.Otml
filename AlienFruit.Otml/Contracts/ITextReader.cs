using System;

namespace AlienFruit.Otml
{
    internal interface ITextReader : IDisposable
    {
        event EventHandler OnNewLine;

        CurrentCharLocation CurrentLocation { get; }

        int Read();

        string ReadLine();

        string PeekToNextLine();

        void ToLineEnd();
    }
}