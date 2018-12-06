using AlienFruit.Otml.Models;
using System;
using System.Collections.Generic;

namespace AlienFruit.Otml.Factories
{
    internal class OtmlNodeFactory : INodeFactory
    {
        public void AddChild(OtmlNode toParrent, OtmlNode child)
        {
            switch (toParrent.Type)
            {
                case NodeType.Object:
                case NodeType.Property:
                    (toParrent as Node).AddChild(child);
                    break;

                case NodeType.Value: throw new InvalidOperationException("Is't possible add any children to property");
                default: throw new InvalidOperationException("Unknown node type");
            }
        }

        public OtmlNode CreateNode(NodeType type, string name)
        {
            switch (type)
            {
                case NodeType.Object: return new ObjectNode(name);
                case NodeType.Property: return new PropertyNode(name);
                case NodeType.Value: throw new InvalidOperationException("Use \"CreateValue\" method for \"Value\" type");
                default: throw new InvalidOperationException("Unknown node type");
            }
        }

        public OtmlNode CreateNode(NodeType type, string name, IEnumerable<OtmlNode> children)
        {
            switch (type)
            {
                case NodeType.Object: return new ObjectNode(name, children);
                case NodeType.Property: return new PropertyNode(name, children);
                case NodeType.Value: throw new InvalidOperationException("Use \"CreateValue\" method for \"Value\" type");
                default: throw new InvalidOperationException("Unknown node type");
            }
        }

        public OtmlNode CreateValue(string value, bool isMultiline = false)
            => new ValueNode(value, isMultiline);
    }
}