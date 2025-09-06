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

        protected abstract bool GetMultilineTextState();

        protected abstract bool IsArrayProperty();

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
                        if(value != null)
                            throw new InvalidOperationException($"The {Type} can't has a value");
                        return null;
                    default: return value;
                }
            }
        }

        public bool IsMultilineText
        {
            get
            {
                var isMultiline = GetMultilineTextState();
                switch (Type)
                {
                    case NodeType.Object:
                    case NodeType.Property:
                        return isMultiline
                            ? throw new InvalidOperationException($"The {Type} cannot be multiline text")
                            : false;

                    default: return isMultiline;
                }
            }
        }

        public bool IsArray
        {
            get
            {
                var isArray = IsArrayProperty();
                switch (Type)
                {
                    case NodeType.Object:
                    case NodeType.Value:
                        return isArray
                            ? throw new InvalidOperationException($"The {Type} cannot be array")
                            : false;

                    default: return isArray;
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