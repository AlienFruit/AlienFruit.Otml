using System.Collections.Generic;
using System.IO;

namespace AlienFruit.Otml
{
    public interface IUnparser
    {
        string Unparse(IEnumerable<OtmlNode> tree);

        void Unparse(IEnumerable<OtmlNode> tree, Stream toStream, bool leaveOpen = false);

        INodeFactory GetNodeFactory();
    }
}