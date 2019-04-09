using AlienFruit.Otml.Domain.Version1v0;
using AlienFruit.Otml.Exceptions;
using AlienFruit.Otml.Models;
using AlienFruit.Otml.Readers;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AlienFruit.Otml.Tests
{
    public class OtmlParserTests
    {
        private readonly string testDataFile = TestContext.CurrentContext.TestDirectory + @"\OtmlTestData.py";

        [Test]
        public void TestMethod1()
        {
            Stopwatch sw = new Stopwatch();

            var str = File.ReadAllText(testDataFile);
            sw.Start();
            //var result = OtmlParser.Parse(str);
            sw.Stop();

            //Debug.WriteLine($"ellapsed1: {sw.ElapsedTicks}");
            string result4;
            using (var stream = File.OpenRead(testDataFile))
            {
                sw.Reset();
                sw.Start();
                var result2 = new OtmlParser(new StreamTextReader(stream)).Parse();
                sw.Stop();
                Debug.WriteLine($"ellapsed2: {sw.ElapsedTicks}");

                result4 = new OtmlUnparser(Encoding.UTF8).Unparse(result2);
            }

            //var result3 = OtmlParser.Unparse(result);
            //using (var stream = File.OpenWrite(@"D:\test2.otml"))
            //{
            //    sw.Reset();
            //    sw.Start();
            //    OtmlParser.Unparse(result, stream, Encoding.UTF8);
            //    sw.Stop();
            //    Debug.WriteLine($"ellapsed3: {sw.ElapsedTicks}");
            //}

            //sw.Reset();
            //sw.Start();
            //var result3 = OtmlParser.Unparse(result);
            //sw.Stop();
            //Debug.WriteLine($"ellapsed4: {sw.ElapsedTicks}");
        }

        [Test]
        public void Parse_should_throw_exception_if_script_line_has_space_character_on_left_side()
        {
            // Arrange
            var script = " @property : value";

            // Action
            Action action = () => new OtmlParser(new StringTextReader(script)).Parse();

            // Assert
            action.Should().Throw<OtmlParseException>()
                .WithMessage("Unacceptable space character, should use only a tab character in this plase, in line: 1, position: 1");
        }

        [Test]
        public void Parse_result_from_string_should_be_equal_result_from_stream()
        {
            // Arrange
            var otmlString = File.ReadAllText(testDataFile);
            var result1 = Enumerable.Empty<OtmlNode>();
            var result2 = Enumerable.Empty<OtmlNode>();

            // Action
            result1 = new OtmlParser(new StringTextReader(otmlString)).Parse();
            using (var stream = File.OpenRead(testDataFile))
            {
                result2 = new OtmlParser(new StreamTextReader(stream)).Parse();
            }

            // Assert
            result1.Should().BeEquivalentTo(result2);
        }

        [Test]
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
            var result = new OtmlParser(new StringTextReader(codeString)).Parse();

            // Assert
            result.Should().BeEquivalentTo(source);
        }

        #region IsPropertyHasPlus

        [Test]
        public void IsPropertyHasPlus_should_return_true_if_has_plus_on_right_side()
        {
            // Arrange
            var propertyString = "Property value  +    ";

            // Action
            var result = OtmlParser.IsPropertyHasPlus(propertyString);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
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

        [Test]
        public void TrimProperty_should_return_property_without_spaces_and_qoutes()
        {
            // Arrange
            var propertyString = " \"Property value\" ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("Property value");
        }

        [Test]
        public void TrimProperty_should_not_trim_spaces_inside_qoutes()
        {
            // Arrange
            var propertyString = " \"  Property value  \" ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("  Property value  ");
        }

        [Test]
        public void TrimProperty_should_return_property_without_spaces()
        {
            // Arrange
            var propertyString = " Property value ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("Property value");
        }

        [Test]
        public void TrimProperty_should_return_property_with_quotation_mark_in_right_side()
        {
            // Arrange
            var propertyString = "Property value\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be(propertyString);
        }

        [Test]
        public void TrimProperty_should_return_property_with_shielded_quotation_mark_in_right_side_if_quote_has_shield()
        {
            // Arrange
            var propertyString = "Property value\\\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be(propertyString);
        }

        [Test]
        public void TrimProperty_should_throw_exception_if_property_has_single_quotation_mark_and_has_not_closing_quotation_mark()
        {
            // Arrange
            var propertyString = "\'Property value";
            Action action = () => OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 1), out var offset, out var quotesIndex);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark ('), in line: 1, position: 16");
        }

        [Test]
        public void TrimProperty_should_throw_exception_if_property_has_double_quotation_mark_and_has_not_closing_quotation_mark()
        {
            // Arrange
            var propertyString = "\"Property value";
            Action action = () => OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 2), out var offset, out var quotesIndex);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark (\"), in line: 1, position: 17");
        }

        [Test]
        public void TrimProperty_should_return_property_without_plus_and_quotes()
        {
            // Arrange
            var propertyString = "\"Property value\" + ";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quotesIndex);

            // Assert
            result.Should().Be("Property value");
        }

        [Test]
        public void TrimProperty_should_return_null_quote_char_if_value_is_not_inside_quotes_marks()
        {
            // Arrange
            var propertyString = "Property value\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quoteChar);

            // Assert
            quoteChar.Should().BeNull();
        }

        [Test]
        public void TrimProperty_should_return_single_quote_char_if_property_value_inside_a_single_quotes()
        {
            // Arrange
            var propertyString = "\'Property value\'";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quoteChar);

            // Assert
            quoteChar.Should().Be('\'');
        }

        [Test]
        public void TrimProperty_should_return_double_quote_char_if_property_value_inside_a_double_quotes()
        {
            // Arrange
            var propertyString = "\"Property value\"";

            // Action
            var result = OtmlParser.TrimProperty(propertyString, new CurrentCharLocation(1, 0), out var offset, out var quoteChar);

            // Assert
            quoteChar.Should().Be('\"');
        }

        #endregion TrimProperty

        #region ParsePropertyValue

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_closing_single_quatation_mark_is_missed()

        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\' test property string", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark ('), in line: 1, position: 22");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_closing_double_quotation_mark_is_missed()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\" test property string", new CurrentCharLocation(1, 2), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark (\"), in line: 1, position: 24");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_unexpected_chars_exists_after_closing_quotation_mark()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\" test property string \" unexpected chars", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Missing closing quotation mark (\"), in line: 1, position: 41");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_property_string_contains_only_whitespaces()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("    ", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Empty property cannot be parsed, in line: 1, position: 0");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_value_has_unshielded_double_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue(" value \" with double quote", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char (\"), in line: 1, position: 7");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_value_has_unshielded_single_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue(" value ' with double quote", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char ('), in line: 1, position: 7");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_value_inside_double_quotes_has_unshielded_double_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("\" value \" with double quote \"", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char (\"), in line: 1, position: 8");
        }

        [Test]
        public void ParsePropertyValue_should_throw_exception_if_value_inside_single_quotes_has_unshielded_single_quote()
        {
            // Arrange
            Action action = () => OtmlParser.ParsePropertyValue("' value ' with double quote '", new CurrentCharLocation(1, 0), out bool hasPlus, out int posOffset);

            // Assert
            action.Should().Throw<OtmlParseException>().WithMessage("Unshielded quotation mark char ('), in line: 1, position: 8");
        }

        [Test]
        public void ParsePropertyValue_hasPlus_should_return_true_if_property_has_a_plus_char_after_closing_quotation_mark()
        {
            // Arrange
            var propertyString = " \"Property value\"+ ";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            hasPlus.Should().BeTrue();
        }

        [Test]
        public void ParsePropertyValue_hasPlus_should_return_true_if_property_has_a_plus_char_in_the_end_of_line()
        {
            // Arrange
            var propertyString = "Property value + ";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            hasPlus.Should().BeTrue();
        }

        [Test]
        public void ParsePropertyValue_hasPlus_should_return_false_if_property_has_a_plus_char_inside_the_quotes()
        {
            // Arrange
            var propertyString = "\' Property, value + \' ";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            hasPlus.Should().BeFalse();
        }

        [Test]
        public void ParsePropertyValue_should_return_value_with_shielded_quotation_mark_if_propery_is_not_inside_quotes()
        {
            // Arrange
            var propertyString = "\\\"pro\\'perty";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be("\"pro\'perty");
        }

        [Test]
        public void ParsePropertyValue_should_return_value_without_shielded_quotetion_mark_if_property_is_not_inside_quotes()
        {
            // Arrange
            var propertyString = "\"\\\"property\"";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be("\"property");
        }

        [Test]
        public void ParsePropertyValue_should_return_value_with_single_quote_if_property_is_inside_double_quotes()
        {
            // Arrange
            var propertyString = "\" property value with ' \"";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be(" property value with ' ");
        }

        [Test]
        public void ParsePropertyValue_should_return_value_with_double_quote_if_property_is_inside_single_quotes()
        {
            // Arrange
            var propertyString = "' property value with \" '";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be(" property value with \" ");
        }

        [Test]
        public void ParsePropertyValue_should_return_value_with_single_back_slash_in_the_end()
        {
            // Arrange
            var propertyString = @"D:\asdaad\asdasdasdasd\\'";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be(@"D:\asdaad\asdasdasdasd\'");
        }

        [Test]
        public void ParsePropertyValue_should_return_value_without_quotes_if_value_is_in_quotes_and_last_char_after_last_quote_is_back_slash()
        {
            // Arrange
            var propertyString = @"'D:\asdaad\asdasdasdasd\'";
            var location = new CurrentCharLocation();

            // Action
            var result = OtmlParser.ParsePropertyValue(propertyString, location, out bool hasPlus, out int posOffset);

            // Assert
            result.Should().Be(@"D:\asdaad\asdasdasdasd\");
        }

        #endregion ParsePropertyValue

        #region SplitInlineProperies

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void GetNameAndValues_test1()
        {
            // Arrange
            var value = "name1 : value1, value2, val\\'ue3 #comment";

            // Action
            var result = OtmlParser.GetNameAndValues(value);
        }

        #endregion GetNameAndValues
    }
}