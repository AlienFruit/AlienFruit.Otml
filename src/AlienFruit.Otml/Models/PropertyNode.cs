using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Models
{
    internal class PropertyNode : Node
    {
        private readonly bool hasArraySyntax;

        public PropertyNode(string name, bool hasArraySyntax = false) : base(name)
        {
            this.hasArraySyntax = hasArraySyntax;
        }

        public PropertyNode(string name, IEnumerable<OtmlNode> children, bool hasArraySyntax = false)
            : base(name, children)
        {
            this.hasArraySyntax = hasArraySyntax;
        }

        public override NodeType Type => NodeType.Property;

        public override void AddChild(OtmlNode child) => base.children.Add(child);

        protected override bool IsArrayProperty() => this.hasArraySyntax || base.Children.Count() > 1;
    }
}