using System.Collections.Generic;

namespace AlienFruit.Otml.Models
{
    internal class PropertyNode : Node
    {
        public PropertyNode(string name) : base(name)
        {
        }

        public PropertyNode(string name, IEnumerable<OtmlNode> children)
            : base(name, children)
        {
        }

        public override NodeType Type => NodeType.Property;

        public override void AddChild(OtmlNode child) => base.children.Add(child);
    }
}