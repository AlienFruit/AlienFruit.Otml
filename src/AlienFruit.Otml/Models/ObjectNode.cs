using System.Collections.Generic;

namespace AlienFruit.Otml.Models
{
    internal class ObjectNode : Node
    {
        public ObjectNode(string name) : base(name)
        {
        }

        public ObjectNode(string name, IEnumerable<OtmlNode> children) : base(name, children)
        {
        }

        public override NodeType Type => NodeType.Object;

        public override void AddChild(OtmlNode child) => base.children.Add(child);
    }
}