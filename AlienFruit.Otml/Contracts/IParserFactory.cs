using System.IO;

namespace AlienFruit.Otml
{
    public interface IParserFactory
    {
        IParser GetParser(string otmlText);

        IParser GetParser(Stream stream, bool leaveOpen = false);
    }
}