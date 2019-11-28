using System;
using System.Collections.Generic;

namespace AlienFruit.Otml
{
    public interface IParser : IDisposable
    {
        IEnumerable<OtmlNode> Parse();
    }
}