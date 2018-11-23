using AlienFruit.Otml.Serializer.Attributes;
using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class ClassFormatter<T> : IFormatter<T> where T : class
    {
        private readonly IResolver resolver;

        public ClassFormatter(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public T Deserialize(IEnumerable<OtmlNode> node)
        {
            if (!node.Any())
                throw new OtmlDeserializeException($"Cannot deserialize class \"{typeof(T)}\" from empty node");
            if (node.Count() > 1)
                throw new OtmlDeserializeException("A class can be deserialized from only single node");
            if (node.All(x => !(x.Type == NodeType.Object)))
                throw new OtmlDeserializeException("A class can be deserialized from only object type node");

            var type = typeof(T);
            var classNode = node.Single();

            if (type.Name != classNode.Name)
                throw new OtmlDeserializeException($"Wrong class '{type.Name}' instead required class '{classNode.Name}'");

            var result = Activator.CreateInstance(type);

            foreach (var propertyNode in classNode.Children)
            {
                if (!(propertyNode.Type == NodeType.Property))
                    throw new OtmlDeserializeException($"Expected a property node, but founded the value node in class {type.Name}");
                var property = type.GetProperty(propertyNode.Name)
                    ?? throw new OtmlDeserializeException($"Property {propertyNode.Name} was not found in class {type.Name}");
                if (!property.CanWrite)
                    throw new OtmlDeserializeException($"Cannot set value fot property {property.Name}, because property has not setter");
                var formatter = this.resolver.GetFormatter(property.PropertyType);

                var r = formatter.DeserializeObject(propertyNode.Children);
                property.SetValue(result, r);
            }

            return (T)result;
        }

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> Serialize(T value, INodeFactory nodeFactory)
        {
            if (value is null)
                return Enumerable.Empty<OtmlNode>();
            var type = typeof(T);
            var result = nodeFactory.CreateNode(NodeType.Object, type.Name);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!property.CanRead)
                    continue;
                if (HasIgnoreAttrubute(property))
                    continue;
                var propertyValue = property.GetValue(value);
                if (propertyValue is null)
                    continue;
                var formatter = this.resolver.GetFormatter(property.PropertyType);
                var propertyNode = nodeFactory.CreateNode(NodeType.Property, property.Name, formatter.SerializeObject(propertyValue, nodeFactory));
                nodeFactory.AddChild(result, propertyNode);
            }
            return result.Singleton();
        }

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((T)value, nodeFactory);

        private static bool HasIgnoreAttrubute(PropertyInfo property)
            => property.GetCustomAttribute(typeof(SerializerIgnoreAttribute)) != null
            || property.GetCustomAttribute(typeof(IgnoreDataMemberAttribute)) != null;
    }
}