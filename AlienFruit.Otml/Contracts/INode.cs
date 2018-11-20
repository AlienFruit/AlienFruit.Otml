using System.Collections.Generic;

namespace AlienFruit.Otml
{
    public enum NodeType
    {
        Object,
        Property,
        Value
    }

    public interface INode
    {
        string Name { get; }
        string Value { get; }
        NodeType Type { get; }
        bool IsMultiline { get; }
        IEnumerable<INode> Children { get; }

        void AddChild(INode child);
    }
}