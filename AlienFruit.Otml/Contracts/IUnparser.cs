using System.Collections.Generic;
using System.IO;

namespace AlienFruit.Otml
{
    public interface IUnparser
    {
        string Unparse(IEnumerable<INode> tree);

        void Unparse(IEnumerable<INode> tree, Stream toStream, bool leaveOpen);

        INodeFactory GetNodeFactory();
    }
}