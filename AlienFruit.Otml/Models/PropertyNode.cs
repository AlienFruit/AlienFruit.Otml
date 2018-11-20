using System.Collections.Generic;

namespace AlienFruit.Otml.Models
{
    internal class PropertyNode : Node
    {
        public PropertyNode(string name) : base(name)
        {
        }

        public PropertyNode(string name, IEnumerable<INode> children)
            : base(name, children)
        {
        }

        public override NodeType Type => NodeType.Property;

        public override bool IsMultiline => false; //TODO: propertyNode cannot have this property (only value property)

        public override void AddChild(INode child) => base.children.Add(child);
    }
}