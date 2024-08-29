using AlienFruit.Otml.Serializer.Exceptions;
using AlienFruit.Otml.Serializer.Formatters;
using AlienFruit.Otml.Serializer.Utils;
using AutoFixture;
using FluentAssertions;
using Moq;
using System;
using System.Drawing;
using System.Linq;
using Xunit;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    public class ColorFormatterTests : BaseFormatterTest
    {
        private static string ColorToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

        [Fact]
        public void Serialize_ShouldReturnsHexColor_IfValueIsCorrectHexCode()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var a = fixture.Create<int>();
            var r = fixture.Create<int>();
            var g = fixture.Create<int>();
            var b = fixture.Create<int>();
            var color = Color.FromArgb(a, r, g, b);
            var nodeFactory = new Mock<INodeFactory>();

            // Act
            var result = formatter.Serialize(color, nodeFactory.Object);

            // Assert
            nodeFactory.Verify(x => x.CreateValue(It.Is<string>(arg => arg == ColorToHex(color)), It.IsAny<bool>()));
        }

        [Fact]
        public void Deserialize_ShouldReturnsColor_IfValuIsHexColor()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var a = fixture.Create<int>();
            var r = fixture.Create<int>();
            var g = fixture.Create<int>();
            var b = fixture.Create<int>();
            var color = Color.FromArgb(a, r, g, b);
            var node = new StubNode
            {
                Value = ColorToHex(color),
                NodeType = NodeType.Value
            };

            // Act
            var result = formatter.Deserialize(node.Singleton());

            // Asser 
            result.Should().Be(color);
        }

        [Fact]
        public void Deserialize_ShouldThrowException_IfNodeHasMoreThenOneValue()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var node = fixture.Build<StubNode>()
                .CreateMany(2);

            // Act
            Action act = () => formatter.Deserialize(node);

            // Assert
            act.Should().Throw<OtmlDeserializeException>()
                .WithMessage($"The Color value can be built from only one node, but founded {node.Count()}");
        }

        [Fact]
        public void Deserialize_ShouldThrowException_IfNodeHasNoAnyValues()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var node = Enumerable.Empty<StubNode>();

            // Act
            Action act = () => formatter.Deserialize(node);

            // Assert
            act.Should().Throw<OtmlDeserializeException>()
                .WithMessage("The Color value cannot be null");
        }

        [Fact]
        public void Deserialize_ShouldThrowException_IfNodeTypeisNotValue()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var node = new StubNode
            {
                NodeType = NodeType.Property
            };

            // Act
            Action act = () => formatter.Deserialize(node.Singleton());

            // Assert
            act.Should().Throw<OtmlDeserializeException>()
                .WithMessage("The Color nodes should be a ValueNode type");
        }

        [Fact]
        public void Deserialize_ShouldThrowException_IfNodeValueHasMoreThenNineChars()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var node = new StubNode
            {
                Value = fixture.Create<string>(),
                NodeType = NodeType.Value
            };

            // Act
            Action act = () => formatter.Deserialize(node.Singleton());

            // Assert
            act.Should().Throw<OtmlDeserializeException>()
                .WithMessage("The Color value has incorrect format");
        }

        [Fact]
        public void Deserialize_ShouldThrowException_IfNodeValueIsIncorrectColorHex()
        {
            // Arrange
            var formatter = new ColorFormatter();
            var node = new StubNode
            {
                Value = "#FFAA00FT",
                NodeType = NodeType.Value
            };

            // Act
            Action act = () => formatter.Deserialize(node.Singleton());

            // Assert
            act.Should().Throw<OtmlDeserializeException>()
                .WithMessage("The Color value has incorrect format");
        }
    }
}
