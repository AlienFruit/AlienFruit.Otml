using AlienFruit.Otml.Serializer.Formatters;
using AutoFixture;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    public class ArrayFormatterTests : BaseFormatterTest
    {
        [Fact]
        public void Serialize_should_return_tree_model_if_value_is_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            var formatter = new ArrayFormatter<string>(resolver);
            var value = fixture.CreateMany<string>().ToArray();

            // Action
            var result = formatter.Serialize(value, null);

            // Assert
            result.Select((x, i) => value[i] == x.Value).All(x => true).Should().BeTrue();
        }

        [Fact]
        public void SerilizeObject_should_return_tree_model_if_value_is_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            var formatter = new ArrayFormatter<string>(resolver);
            var value = fixture.CreateMany<string>().ToArray();

            // Action
            var result = formatter.SerializeObject(value, null);

            // Assert
            result.Select((x, i) => value[i] == x.Value).All(x => true).Should().BeTrue();
        }

        [Fact]
        public void Deserialize_should_return_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            var formatter = new ArrayFormatter<string>(resolver) as IFormatter<string[]>;
            var value = fixture.CreateMany<StubNode>();

            // Action
            string[] result = formatter.Deserialize(value);

            // Assert
            value.Select((x, i) => result[i] == x.Value).All(x => true).Should().BeTrue();
        }

        [Fact]
        public void DeserializeObject_should_return_string_array()
        {
            // Arrange
            var resolver = new StubResolver<string>(fixture);
            var formatter = new ArrayFormatter<string>(resolver);
            var value = fixture.CreateMany<StubNode>();

            // Action
            string[] result = formatter.DeserializeObject(value) as string[];

            // Assert
            value.Select((x, i) => result[i] == x.Value).All(x => true).Should().BeTrue();
        }
    }
}