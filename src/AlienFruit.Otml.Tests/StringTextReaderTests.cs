using AlienFruit.Otml.Readers;
using FluentAssertions;
using Xunit;
using System;
using System.Text;

namespace AlienFruit.Otml.Tests
{
    public class StringTextReaderTests
    {
        [Fact]
        public void Read_should_return_char_if_text_is_not_empty()
        {
            // Arrange
            var text = "1";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            var result = (char)reader.Read();

            // Assert
            result.Should().Be('1');
        }

        [Fact]
        public void Read_should_return_negative_value_if_text_is_empty()
        {
            // Arrange
            var text = "";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            var result = reader.Read();

            // Assert
            result.Should().Be(-1);
        }

        [Fact]
        public void Read_should_return_new_line_char_if_was_called_ToLineEnd()
        {
            // Arrange
            var text = "f" + Environment.NewLine + "s";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            reader.ToLineEnd();
            var result = (char)reader.Read();

            // Assert
            result.Should().Be('s');
        }

        [Fact]
        public void PeekToNextLine_should_not_change_current_position()
        {
            // Arrange
            var text = "first line" + Environment.NewLine + "second line";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            var line = reader.PeekToNextLine();
            var result = (char)reader.Read();

            // Assert
            line.Should().Be("first line");
            result.Should().Be('f');
        }

        [Fact]
        public void Location_should_contain_correct_lines_count()
        {
            // Arrange
            var text = "first line" + Environment.NewLine + "second line" + Environment.NewLine + "third line";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            while (reader.Read() >= 0) { }

            // Assert
            reader.CurrentLocation.Line.Should().Be(3);
        }

        [Fact]
        public void Location_should_contain_correct_lines_count_if_text_has_only_new_line_chars()
        {
            // Arrange
            var text = Environment.NewLine + Environment.NewLine + Environment.NewLine;
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            while (reader.Read() >= 0) { }

            // Assert
            reader.CurrentLocation.Line.Should().Be(4);
        }

        [Fact]
        public void ReadLine_should_return_current_line()
        {
            // Arrange
            var text = "first line" + Environment.NewLine + "second line";
            using var reader = new StringTextReader(text) as ITextReader;
            
            // Action
            var result = reader.ReadLine();

            // Assert
            result.Should().Be("first line");
        }

        [Fact]
        public void PeekToNextLine_should_return_last_line_if_it_has_no_ending_new_line()
        {
            // Arrange
            var text = "first line\nsecond line";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            var first = reader.ReadLine();
            var result = reader.PeekToNextLine();

            // Assert
            first.Should().Be("first line");
            result.Should().Be("second line");
        }

        [Fact]
        public void ToLineEnd_should_not_throw_when_last_line_has_no_new_line()
        {
            // Arrange
            var text = "first line\nlast line";
            using var reader = new StringTextReader(text) as ITextReader;

            // Action
            reader.ReadLine();
            Action action = () => reader.ToLineEnd();

            // Assert
            action.Should().NotThrow();
            reader.Read().Should().Be(-1);
        }

        // =====================================================================
        // Design-decision record: the OTML line break is only '\n'
        // (and '\r\n'). A lone '\r' is an ordinary character; this is not a bug.
        // =====================================================================

        [Fact]
        public void ReadLine_should_not_treat_lone_cr_as_new_line()
        {
            // OTML is a new format; backward compatibility with classic
            // Mac OS line endings ('\r' without '\n') is not supported.
            using var reader = new StringTextReader("a\rb") as ITextReader;

            reader.ReadLine().Should().Be("a\rb");
        }

        // =====================================================================
        // Problem cases — EXPECTED TO FAIL, they reveal known issues.
        // The class is intentionally not being fixed: solutions will come later.
        // =====================================================================

        [Fact]
        public void ReadLine_should_return_null_at_end_of_text()
        {
            // CONTRACT PROBLEM: System.Text.TextReader.ReadLine returns null at EOF;
            // here it returns an empty string, i.e. "endless empty lines" past the end of the text.
            using var reader = new StringTextReader("") as ITextReader;

            reader.ReadLine().Should().BeNull();
        }

        [Fact]
        public void Read_should_not_expose_bom_as_content_char()
        {
            // ASYMMETRY PROBLEM: StreamTextReader (via StreamReader) consumes the BOM,
            // while StringTextReader returns U+FEFF as the first character of the line —
            // the same document parses differently from a string and from a stream.
            using var reader = new StringTextReader("\uFEFFabc") as ITextReader;

            ((char)reader.Read()).Should().Be('a');
        }

        // =====================================================================
        // Coverage tests of correct behavior (should pass)
        // =====================================================================

        [Fact]
        public void ReadLine_should_strip_cr_only_for_crlf()
        {
            using var reader = new StringTextReader("a\r\nb") as ITextReader;

            reader.ReadLine().Should().Be("a");
            reader.ReadLine().Should().Be("b");
        }

        [Fact]
        public void PeekToNextLine_should_strip_cr_for_crlf_line()
        {
            using var reader = new StringTextReader("a\r\nb") as ITextReader;

            reader.PeekToNextLine().Should().Be("a");
            reader.ReadLine().Should().Be("a"); // peek did not move the position
        }

        [Fact]
        public void PeekToNextLine_should_return_rest_of_current_line_after_partial_read()
        {
            using var reader = new StringTextReader("first line\nsecond line") as ITextReader;

            ((char)reader.Read()).Should().Be('f');

            reader.PeekToNextLine().Should().Be("irst line");
            reader.PeekToNextLine().Should().Be("irst line"); // repeated peek is stable
            reader.ReadLine().Should().Be("irst line");
        }

        [Fact]
        public void PeekToNextLine_at_end_of_text_should_return_empty_string()
        {
            using var reader = new StringTextReader("a\n") as ITextReader;

            reader.ReadLine().Should().Be("a");
            reader.PeekToNextLine().Should().Be("");
            reader.Read().Should().Be(-1);
        }

        [Fact]
        public void Full_read_should_reproduce_original_characters()
        {
            var text = "a\nbb\r\nccc=dé→\t";
            using var reader = new StringTextReader(text) as ITextReader;

            var sb = new StringBuilder();
            int c;
            while ((c = reader.Read()) >= 0)
                sb.Append((char)c);

            sb.ToString().Should().Be(text);
        }

        [Fact]
        public void OnNewLine_should_fire_exactly_once_per_lf()
        {
            using var reader = new StringTextReader("a\nb\nc") as ITextReader;
            var fired = 0;
            reader.OnNewLine += (_, _) => fired++;

            while (reader.Read() >= 0) { }

            fired.Should().Be(2);
        }

        [Fact]
        public void OnNewLine_should_not_fire_during_peek()
        {
            using var reader = new StringTextReader("a\nb") as ITextReader;
            var fired = 0;
            reader.OnNewLine += (_, _) => fired++;

            reader.PeekToNextLine().Should().Be("a");
            reader.PeekToNextLine().Should().Be("a");
            fired.Should().Be(0);
        }

        [Fact]
        public void ToLineEnd_should_move_to_start_of_next_line_for_crlf()
        {
            using var reader = new StringTextReader("a\r\nb") as ITextReader;

            reader.ToLineEnd();

            ((char)reader.Read()).Should().Be('b');
            reader.CurrentLocation.Line.Should().Be(2);
        }

        [Fact]
        public void CurrentLocation_should_track_line_and_position_through_document()
        {
            using var reader = new StringTextReader("ab\r\ncd") as ITextReader;

            ((char)reader.Read()).Should().Be('a');
            reader.CurrentLocation.Line.Should().Be(1);
            reader.CurrentLocation.Position.Should().Be(1);

            ((char)reader.Read()).Should().Be('b');
            reader.CurrentLocation.Position.Should().Be(2);

            ((char)reader.Read()).Should().Be('\r');
            reader.CurrentLocation.Position.Should().Be(3);

            ((char)reader.Read()).Should().Be('\n');
            reader.CurrentLocation.Line.Should().Be(2);
            reader.CurrentLocation.Position.Should().Be(0);

            ((char)reader.Read()).Should().Be('c');
            reader.CurrentLocation.Position.Should().Be(1);
        }

        [Fact]
        public void ReadLine_should_return_empty_strings_for_empty_lines()
        {
            using var reader = new StringTextReader("a\n\nb") as ITextReader;

            reader.ReadLine().Should().Be("a");
            reader.ReadLine().Should().Be("");
            reader.ReadLine().Should().Be("b");
        }

        [Fact]
        public void Parser_like_flow_peek_then_readline_should_be_consistent()
        {
            // How OtmlParser uses it: read the first char, peek, then read the rest of the line.
            using var reader = new StringTextReader("abc\ndef") as ITextReader;

            ((char)reader.Read()).Should().Be('a');
            reader.PeekToNextLine().Should().Be("bc");
            reader.ReadLine().Should().Be("bc"); // ReadLine consumed '\n'
            reader.CurrentLocation.Line.Should().Be(2);
            ((char)reader.Read()).Should().Be('d');
        }

        [Fact]
        public void Unicode_content_should_survive_roundtrip()
        {
            using var reader = new StringTextReader("привет\nмир") as ITextReader;

            reader.ReadLine().Should().Be("привет");
            reader.ReadLine().Should().Be("мир");
        }

        [Fact]
        public void Empty_text_should_read_as_eof()
        {
            using var reader = new StringTextReader("") as ITextReader;

            reader.PeekToNextLine().Should().Be("");
            reader.CurrentLocation.Line.Should().Be(1);
            reader.CurrentLocation.Position.Should().Be(0);
        }

        [Fact]
        public void Constructor_should_treat_null_text_as_empty()
        {
            using var reader = new StringTextReader(null) as ITextReader;

            reader.Read().Should().Be(-1);
        }

        [Fact]
        public void Read_after_dispose_should_not_throw()
        {
            var reader = new StringTextReader("abc") as ITextReader;
            reader.Dispose();
            reader.Dispose(); // repeated Dispose is safe

            Action action = () => reader.Read();
            action.Should().NotThrow();
        }
    }
}