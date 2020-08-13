using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Formatters
{
    internal class ColorFormatter : IFormatter<Color>
    {
        public Color Deserialize(IEnumerable<OtmlNode> node)
        {
            if (node.Count() > 1)
                throw new OtmlDeserializeException($"The Color value can be built from only one node, but founded {node.Count()}");

            if (node.Count() == 0)
                throw new OtmlDeserializeException("The Color value cannot be null");

            if (node.First().Type != NodeType.Value)
                throw new OtmlDeserializeException("The Color nodes should be a ValueNode type");

            var value = node.First().Value;

            if (value.Length != 9 || value.First() != '#')
                throw new OtmlDeserializeException("The Color value has incorrect format");

            if(Int32.TryParse(string.Concat(value.Skip(1)), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result) == false)
                throw new OtmlDeserializeException($"The Color value has incorrect format");

            return Color.FromArgb(result);
        }

        public object DeserializeObject(IEnumerable<OtmlNode> value) => Deserialize(value);

        public IEnumerable<OtmlNode> Serialize(Color value, INodeFactory nodeFactory)
            => nodeFactory.CreateValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}").Singleton();

        public IEnumerable<OtmlNode> SerializeObject(object value, INodeFactory nodeFactory) => Serialize((Color)value, nodeFactory);
    }
}
