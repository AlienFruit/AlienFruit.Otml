using AlienFruit.Otml.Serializer.Formatters;
using AutoFixture;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    [Parallelizable(ParallelScope.Children)]
    public class EnumerableFormatterTests : BaseFormatterTest
    {
        [Test]
        public void Serialize_should_return_tree_model_if_value_is_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            IFormatter<IEnumerable<string>> formatter = new EnumerableFormatter<string>(resolver);
            var value = fixture.CreateMany<string>().ToArray();

            // Action
            var result = formatter.Serialize(value, null).ToList();

            // Assert
            result.Select((x, i) => value[i] == x.Value).All(x => true).Should().BeTrue();
        }

        [Test]
        public void SerilizeObject_should_return_tree_model_if_value_is_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            IFormatter<IEnumerable<string>> formatter = new EnumerableFormatter<string>(resolver);
            var value = fixture.CreateMany<string>().ToArray();

            // Action
            var result = formatter.SerializeObject(value, null);

            // Assert
            result.Select((x, i) => value[i] == x.Value).All(x => true).Should().BeTrue();
        }

        [Test]
        public void Deserialize_should_return_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            IFormatter<IEnumerable<string>> formatter = new EnumerableFormatter<string>(resolver);
            var value = fixture.CreateMany<StubNode>();

            // Action
            IEnumerable<string> result = formatter.Deserialize(value);

            // Assert
            value.Select((x, i) => result.ElementAt(i) == x.Value).All(x => true).Should().BeTrue();
        }

        [Test]
        public void DeserializeObject_should_return_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            IFormatter<IEnumerable<string>> formatter = new EnumerableFormatter<string>(resolver);
            var value = fixture.CreateMany<StubNode>();

            // Action
            IEnumerable<string> result = formatter.DeserializeObject(value) as IEnumerable<string>;

            // Assert
            value.Select((x, i) => result.ElementAt(i) == x.Value).All(x => true).Should().BeTrue();
        }
    }
}