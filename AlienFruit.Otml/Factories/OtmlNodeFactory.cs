using AlienFruit.Otml.Models;
using System;
using System.Collections.Generic;

namespace AlienFruit.Otml.Factories
{
    internal class OtmlNodeFactory : INodeFactory
    {
        public INode CreateNode(NodeType type, string name)
        {
            switch (type)
            {
                case NodeType.Object: return new ObjectNode(name);
                case NodeType.Property: return new PropertyNode(name);
                case NodeType.Value: throw new InvalidOperationException("Use \"CreateValue\" method for \"Value\" type");
                default: throw new InvalidOperationException("Unknown node type");
            }
        }

        public INode CreateNode(NodeType type, string name, IEnumerable<INode> children)
        {
            switch (type)
            {
                case NodeType.Object: return new ObjectNode(name, children);
                case NodeType.Property: return new PropertyNode(name, children);
                case NodeType.Value: throw new InvalidOperationException("Use \"CreateValue\" method for \"Value\" type");
                default: throw new InvalidOperationException("Unknown node type");
            }
        }

        public INode CreateValue(string value, bool isPartial = false)
            => new ValueNode(value, isPartial);
    }
}