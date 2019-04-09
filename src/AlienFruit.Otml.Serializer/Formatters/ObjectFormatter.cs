using AlienFruit.Otml.Serializer.Exceptions;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class ObjectFormatter : IFormatter<object>
    {
        private readonly IResolver resolver;

        public ObjectFormatter(IResolver resolver)
        {
            this.resolver = resolver;
        }

        public object Deserialize(IEnumerable<OtmlNode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException("A object can be deserialized from only single node");

            var objectNode = node.Single();

            if (objectNode.Children.Count() < 2)
                return objectNode.Value;

            var result = new ExpandoObject();
            var formatter = this.resolver.GetFormatter(typeof(object));
            foreach (var propertyNode in objectNode.Children)
                AddProperty(result, propertyNode.Name, formatter.DeserializeObject(propertyNode.Children));

            return result;
        }

        public object DeserializeObject(IEnumerable<OtmlNode> node) => Deserialize(node);

        public IEnumerable<OtmlNode> Serialize(object value, INodeFactory nodeFactory)
            => value is null ? Enumerable.Empty<OtmlNode>() : this.resolver.GetFormatter(value.GetType()).SerializeObject(value, nodeFactory);

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize(value, nodeFactory);

        private static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}