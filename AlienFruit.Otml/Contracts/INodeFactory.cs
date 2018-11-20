using System.Collections.Generic;

namespace AlienFruit.Otml
{
    public interface INodeFactory
    {
        INode CreateNode(NodeType type, string name);

        INode CreateNode(NodeType type, string name, IEnumerable<INode> children);

        INode CreateValue(string value, bool isPartial = false);
    }
}