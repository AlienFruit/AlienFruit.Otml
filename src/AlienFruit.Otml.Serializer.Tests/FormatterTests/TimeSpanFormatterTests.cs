using AlienFruit.Otml.Serializer.Formatters;
using AlienFruit.Otml.Serializer.Utils;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    public class TimeSpanFormatterTests : BaseFormatterTest
    {
        [Fact]
        public void Serialize_ShouldReturnsSerializedValue_IfValueIsCorrectTimeSpan()
        {
            // Arrange
            var formatter = new TimeSpanFormatter();
            var timeSpanValue = TimeSpan.FromSeconds(12312323);
            var nodeFactory = new Mock<INodeFactory>();

            var a = timeSpanValue.ToString();

            // Act
            var result = formatter.Serialize(timeSpanValue, nodeFactory.Object);

            // Assert
            nodeFactory.Verify(x => x.CreateValue(It.Is<string>(arg => arg == "142.12:05:23"), It.IsAny<bool>()));
        }

        [Fact]
        public void Deserialize_ShouldReturnsTimeSpan_IfValuIsCorectTimsSpan()
        {
            // Arrange
            var formatter = new TimeSpanFormatter();
            var timeSpanValue = TimeSpan.FromSeconds(322343);
            var node = new StubNode
            {
                Value = timeSpanValue.ToString(),
                NodeType = NodeType.Value
            };

            // Act
            var result = formatter.Deserialize(node.Singleton());

            // Asser 
            result.Should().Be(timeSpanValue);
        }
    }
}
