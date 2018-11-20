using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Models
{
    internal class ValueNode : INode
    {
        private string value;

        public ValueNode(string value, bool isMultiline = false)
        {
            this.IsMultiline = this.IsMergeable = isMultiline;
            this.value = value;
        }

        public string Name => string.Empty;

        public IEnumerable<INode> Children => Enumerable.Empty<INode>();

        public bool IsMultiline { get; }
        public bool IsMergeable { get; private set; }
        public string Value => value;

        public NodeType Type => NodeType.Value;

        public string Description => string.Empty;

        public void AddChild(INode child) => throw new NotImplementedException();

        public void Merge(ValueNode value)
        {
            this.value += Environment.NewLine + value.value;
            this.IsMergeable = value.IsMultiline;
        }

        public override string ToString() => this.value;
    }
}