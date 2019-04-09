using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    /// <summary>
    /// it use ISO8601
    /// ex: "2017-06-26T20:45:00.0700000Z"
    /// </summary>
    internal class DateTimeFormatter : IFormatter<DateTime>
    {
        public DateTimeFormatter()
        {
        }

        public IEnumerable<OtmlNode> Serialize(DateTime value, INodeFactory nodeFactory)
            => nodeFactory.CreateValue(value.ToString("o", CultureInfo.InvariantCulture)).Singleton();

        public DateTime Deserialize(IEnumerable<OtmlNode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"DateTime value should be have only one node, but founded {node.Count()}");
            var valueNode = node.Single();
            if (valueNode.Children.Any())
                throw new OtmlDeserializeException($"DateTime node cannot have any children, but founded {valueNode.Children.Count()}");
            return DateTime.Parse(valueNode.Value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((DateTime)value, nodeFactory);

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);
    }
}