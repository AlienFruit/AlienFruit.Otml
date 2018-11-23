using System.Collections.Generic;

namespace AlienFruit.Otml
{
    public interface IParser
    {
        IEnumerable<OtmlNode> Parse();
    }
}