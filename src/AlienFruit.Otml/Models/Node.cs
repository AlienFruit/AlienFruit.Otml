using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Models
{
    internal abstract class Node : OtmlNode
    {
        protected readonly List<OtmlNode> children;
        private readonly string name;

        public Node(string name)
        {
            this.name = name;
            this.children = new List<OtmlNode>();
        }

        public Node(string name, IEnumerable<OtmlNode> children)
        {
            this.name = name;
            this.children = children.ToList();
        }

        protected override string GetName() => this.name;

        protected override IEnumerable<OtmlNode> GetChildren() => children;

        protected override bool GetMultilineTextState() => false;

        protected override string GetValue() => null;

        public abstract void AddChild(OtmlNode child);

        public void AddChild(IEnumerable<OtmlNode> children) => this.children.AddRange(children);
    }
}