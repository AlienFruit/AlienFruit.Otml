using System;
using System.IO;
using System.Linq;
using System.Text;
using AlienFruit.Otml;
using AlienFruit.Otml.Domain.Version1v0;
using AlienFruit.Otml.Models;
using AlienFruit.Otml.Readers;
using FluentAssertions;
using Xunit;

namespace AlienFruit.Otml.Tests
{
    public class OtmlUnparserTests
    {
        #region NameShielding

        // RESOLVED (2026-11): object/property names were emitted as-is — without
        // quoting or escaping. ':' and '#' characters in a name broke the round-trip:
        // on re-parse the name was cut at the first ':' or truncated at '#'.
        // The fix: the unparser escapes names via ShieldValue, the parser
        // removes the escaping via TrimName.

        [Fact]
        public void Unparse_should_preserve_property_name_with_colon()
        {
            // Arrange
            var source = new OtmlNode[]
            {
                new PropertyNode("a : b", new OtmlNode[] { new ValueNode("v") })
            };

            // Action
            var result = RoundTrip(source);

            // Assert: the property name must reach the reader intact
            result.Single().Name.Should().Be("a : b");
            result.Single().Children.Single().Value.Should().Be("v");
        }

        [Fact]
        public void Unparse_should_preserve_property_name_with_comment_char()
        {
            // Arrange
            var source = new OtmlNode[]
            {
                new PropertyNode("a#b", new OtmlNode[] { new ValueNode("v") })
            };

            // Action
            var result = RoundTrip(source);

            // Assert: before the fix "a#b : v" was parsed as comment truncation,
            // the property turned into a bare value "a"
            result.Single().Type.Should().Be(NodeType.Property);
            result.Single().Name.Should().Be("a#b");
        }

        [Fact]
        public void Unparse_should_preserve_object_name_with_colon()
        {
            // Arrange
            var source = new OtmlNode[]
            {
                new ObjectNode("O:1", new OtmlNode[]
                {
                    new PropertyNode("p", new OtmlNode[] { new ValueNode("v") })
                })
            };

            // Action
            var result = RoundTrip(source);

            // Assert: the name "O:1" must not split into name + inline value
            var obj = result.Single();
            obj.Type.Should().Be(NodeType.Object);
            obj.Name.Should().Be("O:1");
            obj.Children.Should().ContainSingle().Which.Name.Should().Be("p");
        }

        #endregion NameShielding

        #region MultilineNewlines

        // SOLUTION (2026-11): line breaks inside multiline values
        // are normalized to the platform one (Environment.NewLine) — like the whole
        // document, which the unparser writes via AppendLine. Round-trip
        // is stable on a single OS; preserving another OS's line break is not a bug.

        [Fact]
        public void Unparse_should_normalize_LF_newlines_in_multiline_value_to_platform()
        {
            // Arrange
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("a\nb", true) })
            };

            // Action
            var result = RoundTrip(source);

            // Assert
            result.Single().Children.Single().Value.Should().Be("a" + Environment.NewLine + "b");
        }

        [Fact]
        public void Unparse_should_normalize_CRLF_newlines_in_multiline_value_to_platform()
        {
            // Arrange
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("a\r\nb", true) })
            };

            // Action
            var result = RoundTrip(source);

            // Assert
            result.Single().Children.Single().Value.Should().Be("a" + Environment.NewLine + "b");
        }

        #endregion MultilineNewlines

        #region RoundTrip

        // Regression guards: round-trips that are currently correct.

        [Fact]
        public void Unparse_should_roundtrip_value_with_quotes()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("a\"b") })
            };
            RoundTrip(source).Single().Children.Single().Value.Should().Be("a\"b");
        }

        [Fact]
        public void Unparse_should_roundtrip_value_with_comma()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("a,b") })
            };
            RoundTrip(source).Single().Children.Single().Value.Should().Be("a,b");
        }

        [Fact]
        public void Unparse_should_roundtrip_value_with_comment_char()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("a#b") })
            };
            RoundTrip(source).Single().Children.Single().Value.Should().Be("a#b");
        }

        [Fact]
        public void Unparse_should_roundtrip_value_with_leading_object_char()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("@obj") })
            };
            RoundTrip(source).Single().Children.Single().Value.Should().Be("@obj");
        }

        [Fact]
        public void Unparse_should_roundtrip_empty_value()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode(string.Empty) })
            };
            RoundTrip(source).Single().Children.Single().Value.Should().BeEmpty();
        }

        [Fact]
        public void Unparse_should_roundtrip_value_with_trailing_backslash()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("ends\\") })
            };
            RoundTrip(source).Single().Children.Single().Value.Should().Be("ends\\");
        }

        [Fact]
        public void Unparse_should_roundtrip_array_property()
        {
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("v1"), new ValueNode("v2") })
            };

            var result = RoundTrip(source);

            result.Single().IsArray.Should().BeTrue();
            result.Single().Children.Select(x => x.Value).Should().Equal("v1", "v2");
        }

        [Fact]
        public void Unparse_should_roundtrip_object_with_values_and_children()
        {
            var source = new OtmlNode[]
            {
                new ObjectNode("O", new OtmlNode[]
                {
                    new ValueNode("v1"),
                    new PropertyNode("p", new OtmlNode[] { new ValueNode("x") }),
                })
            };

            var result = RoundTrip(source);

            var obj = result.Single();
            obj.Children.Should().HaveCount(2);
            obj.Children.First().Value.Should().Be("v1");
            obj.Children.Last().Name.Should().Be("p");
        }

        private static OtmlNode[] RoundTrip(OtmlNode[] source)
        {
            var code = new OtmlUnparser(Encoding.UTF8).Unparse(source);
            using var parser = new OtmlParser(new StringTextReader(code));
            return parser.Parse().ToArray();
        }

        #endregion RoundTrip

        #region Output

        [Fact]
        public void Unparse_should_write_version_header()
        {
            var code = new OtmlUnparser(Encoding.UTF8).Unparse(new OtmlNode[0]);
            code.Should().StartWith("@@version : 1.0");
        }

        [Fact]
        public void Unparse_should_skip_null_items_in_tree()
        {
            var code = new OtmlUnparser(Encoding.UTF8).Unparse(new OtmlNode[] { null });
            code.Should().StartWith("@@version : 1.0");
        }

        [Fact]
        public void Unparse_to_stream_with_leaveOpen_should_keep_stream_usable()
        {
            using var stream = new MemoryStream();
            var source = new OtmlNode[]
            {
                new PropertyNode("p", new OtmlNode[] { new ValueNode("v") })
            };

            new OtmlUnparser(Encoding.UTF8).Unparse(source, stream, leaveOpen: true);

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            reader.ReadToEnd().Should().Contain("p : v");
        }

        #endregion Output
    }
}
