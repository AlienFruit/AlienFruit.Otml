using AlienFruit.Otml.Serializer.Formatters;
using AlienFruit.Otml.Serializer.Utils;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    public class GuidFormatterTests : BaseFormatterTest
    {
        [Fact]
        public void Serialize_ShouldReturnsSerializedValue_IfValueIsCorrectTimeSpan()
        {
            // Arrange
            var formatter = new GuidFormatter();
            var guidValue = Guid.NewGuid();
            var nodeFactory = new Mock<INodeFactory>();

            // Act
            var result = formatter.Serialize(guidValue, nodeFactory.Object);

            // Assert
            nodeFactory.Verify(x => x.CreateValue(It.Is<string>(arg => arg == guidValue.ToString()), It.IsAny<bool>()));
        }

        [Fact]
        public void Deserialize_ShouldReturnsTimeSpan_IfValuIsCorectTimsSpan()
        {
            // Arrange
            var formatter = new GuidFormatter();
            var guidValue = Guid.NewGuid();
            var node = new StubNode
            {
                Value = guidValue.ToString(),
                NodeType = NodeType.Value
            };

            // Act
            var result = formatter.Deserialize(node.Singleton());

            // Asser 
            result.Should().Be(guidValue);
        }
    }
}
