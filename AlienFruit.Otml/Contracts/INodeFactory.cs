using System.Collections.Generic;

namespace AlienFruit.Otml
{
    public interface INodeFactory
    {
        OtmlNode CreateNode(NodeType type, string name);

        OtmlNode CreateNode(NodeType type, string name, IEnumerable<OtmlNode> children);

        OtmlNode CreateValue(string value, bool isPartial = false);

        void AddChild(OtmlNode toParrent, OtmlNode child);
    }
}