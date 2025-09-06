using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Models
{
    internal class ValueNode : OtmlNode
    {
        private string value;
        private readonly bool isMultiline;

        public ValueNode(string value, bool isMultiline = false)
        {
            this.isMultiline = this.IsMergeable = isMultiline;
            this.value = value;
        }

        protected override IEnumerable<OtmlNode> GetChildren() => Enumerable.Empty<OtmlNode>();

        protected override bool GetMultilineTextState() => this.isMultiline;

        protected override string GetName() => string.Empty;

        protected override string GetValue() => this.value;

        //public string Name => string.Empty;

        //public IEnumerable<INode> Children => Enumerable.Empty<INode>();

        //public bool IsMultiline { get; }

        //public string Value => value;

        //public NodeType Type => NodeType.Value;

        public string Description => string.Empty;
        public bool IsMergeable { get; private set; }

        public override NodeType Type => NodeType.Value;

        //public void AddChild(INode child) => throw new NotImplementedException();

        public void Merge(ValueNode value)
        {
            this.value += Environment.NewLine + value.value;
            this.IsMergeable = value.IsMultilineText;
        }

        public override string ToString() => this.value;

        protected override bool IsArrayProperty() => false;
    }
}