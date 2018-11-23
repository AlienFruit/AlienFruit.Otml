using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml
{
    public enum NodeType
    {
        Object,
        Property,
        Value
    }

    public abstract class OtmlNode
    {
        protected abstract string GetName();

        protected abstract string GetValue();

        protected abstract bool GetMultilineState();

        protected abstract IEnumerable<OtmlNode> GetChildren();

        public abstract NodeType Type { get; }

        public string Name
        {
            get
            {
                var name = GetName();
                switch (Type)
                {
                    case NodeType.Object:
                    case NodeType.Property:
                        return name;

                    default:
                        return string.IsNullOrEmpty(name)
                            ? string.Empty
                            : throw new InvalidOperationException($"{Type} name should be empty");
                }
            }
        }

        public string Value
        {
            get
            {
                var value = GetValue();
                switch (Type)
                {
                    case NodeType.Object:
                    case NodeType.Property:
                        return string.IsNullOrEmpty(value)
                            ? string.Empty
                            : throw new InvalidOperationException($"The {Type} can't have a value");

                    default: return value;
                }
            }
        }

        public bool IsMultiline
        {
            get
            {
                var isMultiline = GetMultilineState();
                switch (Type)
                {
                    case NodeType.Object:
                    case NodeType.Property:
                        return isMultiline
                            ? throw new InvalidOperationException($"The {Type} cannot be multiline")
                            : false;

                    default: return isMultiline;
                }
            }
        }

        public IEnumerable<OtmlNode> Children
        {
            get
            {
                var children = GetChildren();
                switch (Type)
                {
                    case NodeType.Value:
                        return children.Any()
                            ? throw new InvalidOperationException($"The {Type} can't have children")
                            : children;

                    default: return children;
                }
            }
        }
    }
}