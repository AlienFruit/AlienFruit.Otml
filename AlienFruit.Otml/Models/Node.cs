using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Models
{
    internal abstract class Node : INode
    {
        protected readonly List<INode> children;

        public Node(string name)
        {
            this.Name = name;
            this.children = new List<INode>();
        }

        public Node(string name, IEnumerable<INode> children)
        {
            this.Name = name;
            this.children = children.ToList();
        }

        public abstract void AddChild(INode child);

        public abstract NodeType Type { get; }

        public virtual string Name { get; }

        public string Value
        {
            get
            {
                var values = children.Where(x => x.Type == NodeType.Value);
                return values.Count() == 1 ? values.Single().Value : string.Empty;
            }
        }

        public virtual IEnumerable<INode> Children => children;

        public abstract bool IsMultiline { get; }

        public void AddChild(IEnumerable<INode> children) => this.children.AddRange(children);
    }
}