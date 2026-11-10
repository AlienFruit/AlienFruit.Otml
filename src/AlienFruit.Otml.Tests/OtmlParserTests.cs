using AlienFruit.Otml.Domain.Version1v0;
using AlienFruit.Otml.Exceptions;
using AlienFruit.Otml.Models;
using AlienFruit.Otml.Readers;
using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AlienFruit.Otml.Tests
{
    public class OtmlParserTests
    {
        private readonly string testDataFile = @"OtmlTestData.otml";

        [Fact]
        public void Parse_should_throw_exception_if_script_line_has_space_character_on_left_side()
        {
            // Arrange
            var script = " @property : value";
            using var parser = new OtmlParser(new StringTextReader(script));
            
            // Action
            Action action = () => parser.Parse();

            // Assert
            action.Should().Throw<OtmlParseException>()
                .WithMessage("Unacceptable space character, should use only a tab character in this plase, in line: 1, position: 1");
        }

        [Fact]
        public void Parse_result_from_string_should_be_equal_result_from_stream()
        {
            // Arrange
            var otmlString = File.ReadAllText(testDataFile);
            var result2 = Enumerable.Empty<OtmlNode>();
            using var parser = new OtmlParser(new StringTextReader(otmlString));

            // Action
            var result1 = parser.Parse();
            using (var stream = File.OpenRead(testDataFile))
            {
                result2 = new OtmlParser(new StreamTextReader(stream)).Parse();
            }

            // Assert
            result1.Should().BeEquivalentTo(result2);
        }

        [Fact]
        public void Unparsing_then_parsing_should_not_change_source_object()
        {
            var source = new OtmlNode[]
            {
                // Arrange
                new ObjectNode("ObjectNode", new OtmlNode[]
                {
                    new PropertyNode("Single value roperty1", new OtmlNode[]{ new ValueNode("simple value") }),
                    new PropertyNode("MiltilineProperty", new OtmlNode[]
                    {
                        new ValueNode($"First line{Environment.NewLine}Second     line", true),
                        new ValueNode($"No string value"),
                        new ValueNode("   string with many spaces     ")
                    }),
                    new ValueNode("simpleObjectValue"),
                    new ValueNode("string with quotes  \"  ' "),
                    new ValueNode("string with \\ backspase  "),
                    new ValueNode("string with backspace in the end \\")
                    //new PropertyNode("", new OtmlNode)
                }),
                new ObjectNode("Object node", new OtmlNode[]
                {
                    new ValueNode("asdasdasdad"),
                    new PropertyNode("Property", new OtmlNode[]{ new ValueNode("value") })
                }),
            };

            // Action
            var codeString = new OtmlUnparser(Encoding.UTF8).Unparse(source);//new OtmlParser(Encoding.UTF8).Unparse(source);
            using var parser = new OtmlParser(new StringTextReader(codeString));
            var result = parser.Parse();

            // Location is parser metadata: code-built nodes have none. The round-trip
            // contract is about the document structure, not coordinates — normalize.
            ClearLocations(result);

            // Assert
            result.Should().BeEquivalentTo(source);
        }

        private static void ClearLocations(IEnumerable<OtmlNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.Location = null;
                ClearLocations(node.Children);
            }
        }

        [Fact]
        public void Parse_when_input_is_null_should_return_empty_sequence()
        {
            // Arrange
            var parser = new OtmlParser(new StringTextReader(null));

            // Action
            var result = parser.Parse();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Parse_when_input_is_empty_should_return_empty_sequence()
        {
            // Arrange
            var parser = new OtmlParser(new StringTextReader(string.Empty));

            // Action
            var result = parser.Parse();

            // Assert
            result.Should().BeEmpty();
        }

        #region IsPropertyHasPlus

        [Fact]
        public void IsPropertyHasPlus_should_return_true_if_has_plus_on_right_side()
        {
            // Arrange
            var propertyString = "Property value  +    ";

            // Action
            var result = OtmlParser.IsPropertyHasPlus(propertyString);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsPropertyHasPlus_should_return_false_if_property_has_plus_inside_quotetion_marks()
        {
            // Arrange
            var propertyString = "\"Property value  +  \"";

            // Action
            var result = OtmlParser.IsPropertyHasPlus(propertyString);

            // Assert
            result.Should().BeFalse();
        }

        #endregion IsPropertyHasPlus

        #region TrimProperty

        [Fact]
        public void TrimProperty_should_return_property_without_spaces_and_qoutes()
        {
            // Arrange
            var propertyString = " \"Property value\" ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("Property value");
        }

        [Fact]
        public void TrimProperty_should_not_trim_spaces_inside_qoutes()
        {
            // Arrange
            var propertyString = " \"  Property value  \" ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("  Property value  ");
        }

        [Fact]
        public void TrimProperty_should_return_property_without_spaces()
        {
            // Arrange
            var propertyString = " Property value ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("Property value");
        }

        [Fact]
        public void TrimProperty_should_return_property_with_quotation_mark_in_right_side()
        {
            // Arrange
            var propertyString = "Property value\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be(propertyString);
        }

        [Fact]
        public void TrimProperty_should_return_property_with_shielded_quotation_mark_in_right_side_if_quote_has_shield()
        {
            // Arrange
            var propertyString = "Property value\\\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be(propertyString);
        }

        [Fact]
        public void TrimProperty_should_throw_exception_if_property_has_single_quotation_mark_and_has_not_closing_quotation_mark()
        {
            // Arrange
            var propertyString = "\'Property value";
            Action action = () => OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 1), out var offset, out var quotesIndex);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark ('), in line: 1, position: 16");
        }

        [Fact]
        public void TrimProperty_should_throw_exception_if_property_has_double_quotation_mark_and_has_not_closing_quotation_mark()
        {
            // Arrange
            var propertyString = "\"Property value";
            Action action = () => OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 2), out var offset, out var quotesIndex);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark (\"), in line: 1, position: 17");
        }

        [Fact]
        public void TrimProperty_should_return_property_without_plus_and_quotes()
        {
            // Arrange
            var propertyString = "\"Property value\" + ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("Property value");
        }

        [Fact]
        public void TrimProperty_should_return_null_quote_char_if_value_is_not_inside_quotes_marks()
        {
            // Arrange
            var propertyString = "Property value\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quoteChar);

            // Assert
            quoteChar.Should().BeNull();
        }

        [Fact]
        public void TrimProperty_should_return_single_quote_char_if_property_value_inside_a_single_quotes()
        {
            // Arrange
            var propertyString = "\'Property value\'";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quoteChar);

            // Assert
            quoteChar.Should().Be('\'');
        }

        [Fact]
        public void TrimProperty_should_return_double_quote_char_if_property_value_inside_a_double_quotes()
        {
            // Arrange
            var propertyString = "\"Property value\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new SourceLocation(1, 0), out var offset, out var quoteChar);

            // Assert
            quoteChar.Should().Be('\"');
        }

        #endregion TrimProperty

        #region ParsePropertyValue

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_closing_single_quatation_mark_is_missed()

        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\' test property string", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark ('), in line: 1, position: 22");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_closing_double_quotation_mark_is_missed()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\" test property string", new SourceLocation(1, 2), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark (\"), in line: 1, position: 24");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_unexpected_chars_exists_after_closing_quotation_mark()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\" test property string \" unexpected chars", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark (\"), in line: 1, position: 41");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_property_string_contains_only_whitespaces()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("    ", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Empty property cannot be parsed, in line: 1, position: 0");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_value_has_unshielded_double_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue(" value \" with double quote", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char (\"), in line: 1, position: 7");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_value_has_unshielded_single_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue(" value ' with double quote", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char ('), in line: 1, position: 7");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_value_inside_double_quotes_has_unshielded_double_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\" value \" with double quote \"", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char (\"), in line: 1, position: 8");
        }

        [Fact]
        public void ParsePropertyValue_should_throw_exception_if_value_inside_single_quotes_has_unshielded_single_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("' value ' with double quote '", new SourceLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char ('), in line: 1, position: 8");
        }

        [Fact]
        public void ParsePropertyValue_hasPlus_should_return_true_if_property_has_a_plus_char_after_closing_quotation_mark()
        {
            // Arrange
            var propertyString = " \"Property value\"+ ";
            var location = new SourceLocation();

            // Action
            _ = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            hasPlus.Should().BeTrue();
        }

        [Fact]
        public void ParsePropertyValue_hasPlus_should_return_true_if_property_has_a_plus_char_in_the_end_of_line()
        {
            // Arrange
            var propertyString = "Property value + ";
            var location = new SourceLocation();

            // Action
            _ = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            hasPlus.Should().BeTrue();
        }

        [Fact]
        public void ParsePropertyValue_hasPlus_should_return_false_if_property_has_a_plus_char_inside_the_quotes()
        {
            // Arrange
            var propertyString = "\' Property, value + \' ";
            var location = new SourceLocation();

            // Action
            _ = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            hasPlus.Should().BeFalse();
        }

        [Fact]
        public void ParsePropertyValue_should_return_value_with_shielded_quotation_mark_if_propery_is_not_inside_quotes()
        {
            // Arrange
            var propertyString = "\\\"pro\\'perty";
            var location = new SourceLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out _, out _);

            // Assert
            result.Should().Be("\"pro\'perty");
        }

        [Fact]
        public void ParsePropertyValue_should_return_value_without_shielded_quotetion_mark_if_property_is_not_inside_quotes()
        {
            // Arrange
            var propertyString = "\"\\\"property\"";
            var location = new SourceLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be("\"property");
        }

        [Fact]
        public void ParsePropertyValue_should_return_value_with_single_quote_if_property_is_inside_double_quotes()
        {
            // Arrange
            var propertyString = "\" property value with ' \"";
            var location = new SourceLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out _, out _);

            // Assert
            result.Should().Be(" property value with ' ");
        }

        [Fact]
        public void ParsePropertyValue_should_return_value_with_double_quote_if_property_is_inside_single_quotes()
        {
            // Arrange
            var propertyString = "' property value with \" '";
            var location = new SourceLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out _, out _);

            // Assert
            result.Should().Be(" property value with \" ");
        }

        [Fact]
        public void ParsePropertyValue_should_return_value_with_single_back_slash_in_the_end()
        {
            // Arrange
            var propertyString = @"D:\asdaad\asdasdasdasd\\'";
            var location = new SourceLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out _, out _);

            // Assert
            result.Should().Be(@"D:\asdaad\asdasdasdasd\'");
        }

        [Fact]
        public void ParsePropertyValue_should_return_value_without_quotes_if_value_is_in_quotes_and_last_char_after_last_quote_is_back_slash()
        {
            // Arrange
            var propertyString = @"'D:\asdaad\asdasdasdasd\'";
            var location = new SourceLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out _, out _);

            // Assert
            result.Should().Be(@"D:\asdaad\asdasdasdasd\");
        }

        #endregion ParsePropertyValue

        #region SplitInlineProperies

        [Fact]
        public void SplitInlineProperies_should_ignore_comma_char_inside_quotation_and_return_two_values()
        {
            // Arrange
            var firstValue = "\"this is first \' value, 123\" ";
            var secondValue = " this is second value";

            // Action
            var result = OtmlParser.SplitInlineProperies($"{firstValue},{secondValue}");

            // Assert
            result.Count().Should().Be(2);
            result[0].Should().Be(firstValue);
            result[1].Should().Be(secondValue);
        }

        [Fact]
        public void SplitInlineProperies_should_ignore_comma_after_comment_char_and_return_two_values()
        {
            // Arrange
            var firstValue = "\"this is first \' value, 123\"";
            var secondValue = "this is second value";
            var inputValue = $"{firstValue},{secondValue}";

            // Action
            var result = OtmlParser.SplitInlineProperies(inputValue);

            // Assert
            result.Count().Should().Be(2);
            result[0].Should().Be(firstValue);
            result[1].Should().Be("this is second value");
        }

        [Fact]
        public void SplitInlineProperies_test()
        {
            // Arrange
            var firstValue = "\"this is first \' value, 123\"";
            var secondValue = "comma is inside ' second value #comment , with comma";
            var thirdValue = "thirdValue";

            //  value, value, value

            // Action
            var result = OtmlParser.SplitInlineProperies($"{firstValue},{secondValue},{thirdValue}");
        }

        #endregion SplitInlineProperies

        #region GetNameAndValues

        [Fact]
        public void GetNameAndValues_should_return_only_value_if_the_white_space_at_the_end_if_comment_placed_before_colon()
        {
            // Arrange
            var value = "trueValue #: value, value2, value3 ";

            // Action
            var result = OtmlParser.GetNameAndValues(value);

            // Assert
            result.Item1.Should().Be("trueValue ");
            result.Item2.Should().BeNull();
        }

        [Fact]
        public void GetNameAndValues_should_return_only_name_if_comment_placed_after_colon()
        {
            // Arrange
            var value = "name  : # value, value2, value3 ";

            // Action
            var result = OtmlParser.GetNameAndValues(value);

            // Assert
            result.Item1.Should().Be("name  ");
            result.Item2.Should().Be(" ");
        }

        [Fact]
        public void GetNameAndValues_should_return_only_first_name_if_colons_more_then_one()
        {
            // Arrange
            var value = "name1 : name2: name3: value ";

            // Action
            var result = OtmlParser.GetNameAndValues(value);

            // Assert
            result.Item1.Should().Be("name1 ");
            result.Item2.Should().Be(" name2: name3: value ");
        }

        [Fact]
        public void GetNameAndValues_test1()
        {
            // Arrange
            var value = "name1 : value1, value2, val\\'ue3 #comment";

            // Action
            var result = OtmlParser.GetNameAndValues(value);
        }

        #endregion GetNameAndValues

        #region ParseErrorLocation

        // RESOLVED (2026-11): the coordinates in OtmlParseException were wrong.
        //
        // Bug mechanism: the parser reads the document line by line via ReadLine(),
        // which consumes '\n' and increments the line counter. The error
        // coordinate was taken from reader.CurrentLocation AFTER reading the line, so:
        //   * line was inflated by 1 for any line ending with '\n';
        //   * position was counted from the end of the read line (after '\n'
        //     that is 0, and in the lostChar branch even -1), not from the offending char.
        // Fix: the node coordinate is captured BEFORE ReadLine (see OtmlParser.Parse).
        //
        // Coordinates are only available inside Message:
        //   "<text>, in line: <N>, position: <M>"
        // (OtmlParseException has no public Location property).

        [Fact]
        public void Parse_error_should_report_the_line_where_it_occurred()
        {
            // Arrange: the document's single line; an empty value between
            // commas:  p : a , , b
            // Act
            var (line, position, message) = ParseErrorLocation("p : a,,b\n");

            // Assert: there are no other lines; the error must be on line 1.
            // Before the fix: "Value cannot be empty, in line: 2, position: 3"
            line.Should().Be(1, "error is in the single line; message: {0}", message);
        }

        [Fact]
        public void Parse_error_should_report_position_of_offending_char()
        {
            // Arrange: an unshielded quotation mark — the offending char at index 1:  a " b
            // Act
            var (line, position, message) = ParseErrorLocation("a\"b\n");

            // Assert: line 1; position is the offending char's index in the line
            // (0-based, the convention for errors inside values).
            // Before the fix: "Unshielded quotation mark char ("), in line: 2, position: 4"
            line.Should().Be(1, "error is in the single line; message: {0}", message);
            position.Should().Be(1, "position must point at the quotation mark (index 1 in the line); message: {0}", message);
        }

        [Fact]
        public void Parse_error_position_should_never_be_negative()
        {
            // Arrange: two values in a line without a property:  a , b
            // Act
            var (line, position, message) = ParseErrorLocation("a, b\n");

            // Assert: a line can never have a negative column.
            // Before the fix: "Only one value can be in the line without a property, in line: 2, position: -1"
            position.Should().BeGreaterThanOrEqualTo(0, "a coordinate cannot be negative; message: {0}", message);
            line.Should().Be(1, "error is in the single line; message: {0}", message);
        }

        [Fact]
        public void Parse_error_in_middle_of_document_should_report_correct_line()
        {
            // Arrange: an unshielded quotation mark on line 2 of 3:
            //   1: a : 1
            //   2: b : x " y   <- here
            //   3: c : 3
            // Act
            var (line, position, message) = ParseErrorLocation("a : 1\nb : x\"y\nc : 3\n");

            // Assert: before the fix line 3 was reported (a +1 shift).
            line.Should().Be(2, "error is on the second line; message: {0}", message);
        }

        // Parses the document, expects an OtmlParseException and returns its
        // coordinates together with the full message text — so that a failed
        // assertion immediately shows what exactly the parser messed up.
        private static (ulong Line, int Position, string Message) ParseErrorLocation(string document)
        {
            OtmlParseException error = null;
            try
            {
                using var parser = new OtmlParser(new StringTextReader(document));
                parser.Parse().ToList();
            }
            catch (OtmlParseException e)
            {
                error = e;
            }

            error.Should().NotBeNull($"the document \"{document}\" must fail parsing");

            var match = Regex.Match(error.Message, @"in line: (\d+), position: (-?\d+)$");
            match.Success.Should().BeTrue(
                $"the message must contain coordinates, but got: {{0}}", error.Message);

            return (ulong.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), error.Message);
        }

        #endregion ParseErrorLocation

        #region MultilineNewlines

        // DECISION (2026-11): newlines in joined multiline values are
        // platform ones (Environment.NewLine), like those of the whole document: the unparser
        // writes lines via AppendLine. LF/CRLF documents are normalized to
        // the OS newline; round-trip is stable on one machine. Mixed documents
        // (both newline styles in one file) are not supported by the format.

        [Fact]
        public void Multiline_value_in_LF_document_should_join_with_platform_newline()
        {
            // Arrange: a document with LF newlines
            using var parser = new OtmlParser(new StringTextReader("p : a +\n\tb\n"));

            // Action
            var value = parser.Parse().ToList().Single().Children.Single();

            // Assert
            value.Value.Should().Be("a" + Environment.NewLine + "b");
        }

        [Fact]
        public void Multiline_value_in_CRLF_document_should_join_with_platform_newline()
        {
            // Arrange: a document with CRLF newlines
            using var parser = new OtmlParser(new StringTextReader("p : a +\r\n\tb\r\n"));

            // Action
            var value = parser.Parse().ToList().Single().Children.Single();

            // Assert
            value.Value.Should().Be("a" + Environment.NewLine + "b");
        }

        #endregion MultilineNewlines

        #region ParseDocument

        // Safety-net tests: behavior that is currently correct.

        [Fact]
        public void Parse_comment_only_document_should_return_empty_tree()
        {
            using var parser = new OtmlParser(new StringTextReader("# just a comment\n"));
            parser.Parse().Should().BeEmpty();
        }

        [Fact]
        public void Parse_comment_at_eof_without_newline_should_not_throw()
        {
            using var parser = new OtmlParser(new StringTextReader("a : 1\n#tail"));
            parser.Parse().ToList().Should().HaveCount(1);
        }

        [Fact]
        public void Parse_blank_lines_between_properties_should_be_ignored()
        {
            using var parser = new OtmlParser(new StringTextReader("a : 1\n\n\nb : 2\n"));
            parser.Parse().ToList().Should().HaveCount(2);
        }

        [Fact]
        public void Parse_tab_level_jump_should_throw()
        {
            using var parser = new OtmlParser(new StringTextReader("@O\n\t\tx : 1\n"));
            Action action = () => parser.Parse().ToList();
            action.Should().Throw<OtmlParseException>().WithMessage("Unexpected node level*");
        }

        [Fact]
        public void Parse_children_under_plain_value_should_throw()
        {
            using var parser = new OtmlParser(new StringTextReader("plain\n\tx : 1\n"));
            Action action = () => parser.Parse().ToList();
            action.Should().Throw<OtmlParseException>().WithMessage("Value cannot has any properties*");
        }

        [Fact]
        public void Parse_duplicate_property_names_should_be_allowed()
        {
            using var parser = new OtmlParser(new StringTextReader("a : 1\na : 2\n"));
            parser.Parse().ToList().Should().HaveCount(2);
        }

        [Fact]
        public void Parse_object_with_inline_values_and_children_should_build_tree()
        {
            using var parser = new OtmlParser(new StringTextReader("@O : v1, v2\n\tchild : 1\n"));
            var result = parser.Parse().ToList();
            result.Single().Children.Should().HaveCount(3); // v1, v2, child
        }

        [Fact]
        public void Parse_shielded_quote_in_value_should_be_unescaped()
        {
            using var parser = new OtmlParser(new StringTextReader("p : a\\\"b\n"));
            var value = parser.Parse().ToList().Single().Children.Single();
            value.Value.Should().Be("a\"b");
        }

        [Fact]
        public void Parse_inline_list_should_keep_comma_inside_quotes()
        {
            using var parser = new OtmlParser(new StringTextReader("p : \"a,b\",c\n"));
            var values = parser.Parse().ToList().Single().Children.Select(x => x.Value).ToArray();
            values.Should().Equal("a,b", "c");
        }

        [Fact]
        public void Parse_trailing_comma_should_be_tolerated()
        {
            using var parser = new OtmlParser(new StringTextReader("p : a,\n"));
            var values = parser.Parse().ToList().Single().Children.Select(x => x.Value).ToArray();
            values.Should().Equal("a");
        }

        #endregion ParseDocument
    }
}