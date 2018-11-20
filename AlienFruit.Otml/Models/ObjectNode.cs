using System.Collections.Generic;

namespace AlienFruit.Otml.Models
{
    internal class ObjectNode : Node
    {
        public ObjectNode(string name) : base(name)
        {
        }

        public ObjectNode(string name, IEnumerable<INode> children) : base(name, children)
        {
        }

        public override NodeType Type => NodeType.Object;

        public override bool IsMultiline => false; //TODO: objectNode cannot have this property (only value property)

        public override void AddChild(INode child) => base.children.Add(child);
    }
}