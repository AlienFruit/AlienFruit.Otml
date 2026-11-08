using AlienFruit.Otml.Readers;
using FluentAssertions;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AlienFruit.Otml.Tests
{
    public class StreamTextReaderTests
    {
        [Fact]
        public void Read_should_return_char_if_text_is_not_empty()
        {
            // Arrange
            var text = "1";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

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
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

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
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

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
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

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
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

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
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

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
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            using var reader = new StreamTextReader(stream) as ITextReader;

            // Action
            var result = reader.ReadLine();

            // Assert
            result.Should().Be("first line");
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
            using var reader = Create("a\rb");

            reader.ReadLine().Should().Be("a\rb");
        }

        // =====================================================================
        // Problem cases — EXPECTED TO FAIL, they reveal known issues.
        // The class is intentionally not being fixed: solutions will come later.
        // The stream is created as UTF-8 without BOM so that the data matches
        // the string reader.
        // =====================================================================

        private static ITextReader Create(string text)
        {
            var stream = new MemoryStream(new UTF8Encoding(false).GetBytes(text));
            return new StreamTextReader(stream, new UTF8Encoding(false), true);
        }

        [Fact]
        public void ReadLine_should_return_null_at_end_of_text()
        {
            // CONTRACT PROBLEM: System.Text.TextReader.ReadLine returns null at EOF;
            // here it returns an empty string, i.e. "endless empty lines" past the end of the text.
            using var reader = Create("");

            reader.ReadLine().Should().BeNull();
        }

        [Fact]
        public void Read_after_dispose_should_not_throw()
        {
            // PROBLEM: use after Dispose throws ObjectDisposedException
            // instead of a graceful EOF.
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
            var reader = new StreamTextReader(stream, new UTF8Encoding(false), true) as ITextReader;
            reader.Dispose();

            Action action = () => reader.Read();

            action.Should().NotThrow();
            reader.Read().Should().Be(-1);
        }

        [Fact]
        public void ReadLine_after_dispose_should_not_throw()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
            var reader = new StreamTextReader(stream) as ITextReader;
            reader.Dispose();

            Action action = () => reader.ReadLine();

            action.Should().NotThrow();
        }

        [Fact]
        public void Peek_after_dispose_should_not_throw()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
            var reader = new StreamTextReader(stream) as ITextReader;
            reader.Dispose();

            Action action = () => reader.PeekToNextLine();

            action.Should().NotThrow();
        }

        [Fact]
        public void Constructor_with_null_encoding_should_throw()
        {
            // PROBLEM: no argument validation — a null encoding is silently accepted.
            using var stream = new MemoryStream();
            Action action = () => new StreamTextReader(stream, null, true);

            action.Should().Throw<ArgumentNullException>();
        }

        // =====================================================================
        // Coverage tests of correct behavior (should pass)
        // =====================================================================

        [Fact]
        public void ReadLine_should_strip_cr_only_for_crlf()
        {
            using var reader = Create("a\r\nb");

            reader.ReadLine().Should().Be("a");
            reader.ReadLine().Should().Be("b");
        }

        [Fact]
        public void PeekToNextLine_should_strip_cr_for_crlf_line()
        {
            using var reader = Create("a\r\nb");

            reader.PeekToNextLine().Should().Be("a");
            reader.ReadLine().Should().Be("a"); // peek did not move the position
        }

        [Fact]
        public void PeekToNextLine_should_return_rest_of_current_line_after_partial_read()
        {
            using var reader = Create("first line\nsecond line");

            ((char)reader.Read()).Should().Be('f');

            reader.PeekToNextLine().Should().Be("irst line");
            reader.PeekToNextLine().Should().Be("irst line"); // repeated peek is stable
            reader.ReadLine().Should().Be("irst line");
        }

        [Fact]
        public void PeekToNextLine_at_end_of_text_should_return_empty_string()
        {
            using var reader = Create("a\n");

            reader.ReadLine().Should().Be("a");
            reader.PeekToNextLine().Should().Be("");
            reader.Read().Should().Be(-1);
        }

        [Fact]
        public void Full_read_should_reproduce_original_characters()
        {
            var text = "a\nbb\r\nccc=dé→\t";
            using var reader = Create(text);

            var sb = new StringBuilder();
            int c;
            while ((c = reader.Read()) >= 0)
                sb.Append((char)c);

            sb.ToString().Should().Be(text);
        }

        [Fact]
        public void OnNewLine_should_fire_exactly_once_per_lf()
        {
            using var reader = Create("a\nb\nc");
            var fired = 0;
            reader.OnNewLine += (_, _) => fired++;

            while (reader.Read() >= 0) { }

            fired.Should().Be(2);
        }

        [Fact]
        public void OnNewLine_should_not_fire_during_peek()
        {
            using var reader = Create("a\nb");
            var fired = 0;
            reader.OnNewLine += (_, _) => fired++;

            reader.PeekToNextLine().Should().Be("a");
            reader.PeekToNextLine().Should().Be("a");
            fired.Should().Be(0);
        }

        [Fact]
        public void ToLineEnd_should_move_to_start_of_next_line_for_crlf()
        {
            using var reader = Create("a\r\nb");

            reader.ToLineEnd();

            ((char)reader.Read()).Should().Be('b');
            reader.CurrentLocation.Line.Should().Be(2);
        }

        [Fact]
        public void ToLineEnd_should_not_throw_at_eof()
        {
            using var reader = Create("a\nlast");

            reader.ReadLine();
            Action action = () => reader.ToLineEnd();

            action.Should().NotThrow();
            reader.Read().Should().Be(-1);
            reader.ToLineEnd(); // a repeated call is also safe
        }

        [Fact]
        public void CurrentLocation_should_track_line_and_position_through_document()
        {
            using var reader = Create("ab\r\ncd");

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
            using var reader = Create("a\n\nb");

            reader.ReadLine().Should().Be("a");
            reader.ReadLine().Should().Be("");
            reader.ReadLine().Should().Be("b");
        }

        [Fact]
        public void Parser_like_flow_peek_then_readline_should_be_consistent()
        {
            // How OtmlParser uses it: read the first char, peek, then read the rest of the line.
            using var reader = Create("abc\ndef");

            ((char)reader.Read()).Should().Be('a');
            reader.PeekToNextLine().Should().Be("bc");
            reader.ReadLine().Should().Be("bc"); // ReadLine consumed '\n'
            reader.CurrentLocation.Line.Should().Be(2);
            ((char)reader.Read()).Should().Be('d');
        }

        [Fact]
        public void Unicode_content_should_survive_roundtrip()
        {
            using var reader = Create("привет\nмир");

            reader.ReadLine().Should().Be("привет");
            reader.ReadLine().Should().Be("мир");
        }

        [Fact]
        public void PeekToNextLine_then_ToLineEnd_should_not_skip_next_line()
        {
            using var reader = Create("one\ntwo");

            reader.PeekToNextLine().Should().Be("one");
            reader.ToLineEnd();
            reader.ReadLine().Should().Be("two");
        }

        // =====================================================================
        // Stream-specific behavior
        // =====================================================================

        [Fact]
        public void Utf16_stream_with_bom_should_be_detected_by_default_constructor()
        {
            // Without an explicit encoding: StreamReader must detect UTF-16 from the BOM itself.
            var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("привет\nx")).ToArray();
            using var stream = new MemoryStream(bytes);
            using var reader = new StreamTextReader(stream) as ITextReader;

            reader.ReadLine().Should().Be("привет");
            reader.ReadLine().Should().Be("x");
        }

        [Fact]
        public void Utf16_stream_without_bom_should_work_with_explicit_encoding()
        {
            var bytes = Encoding.Unicode.GetBytes("ab\ncd"); // BOM disabled by an explicit Encoding without a preamble
            using var stream = new MemoryStream(bytes);
            using var reader = new StreamTextReader(stream, new UnicodeEncoding(false, false), true) as ITextReader;

            reader.ReadLine().Should().Be("ab");
            reader.ReadLine().Should().Be("cd");
        }

        [Fact]
        public void Utf8_bom_should_be_consumed_by_reader()
        {
            // StreamTextReader via StreamReader consumes the BOM — this is the reference behavior
            // that StringTextReader lacks (an asymmetry between the two implementations).
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("abc")).ToArray();
            using var stream = new MemoryStream(bytes);
            using var reader = new StreamTextReader(stream, Encoding.UTF8, true) as ITextReader;

            ((char)reader.Read()).Should().Be('a');
            reader.ReadLine().Should().Be("bc");
        }

        [Fact]
        public void Invalid_utf8_bytes_should_not_break_reading()
        {
            // 0xFF is an invalid UTF-8 sequence.
            // We expect the reader not to crash but to survive the byte (default decoder).
            using var stream = new MemoryStream(new byte[] { (byte)'a', 0xFF, (byte)'b' });
            using var reader = new StreamTextReader(stream) as ITextReader;

            Action action = () =>
            {
                while (reader.Read() >= 0) { }
            };

            action.Should().NotThrow();
        }

        [Fact]
        public void PeekToNextLine_should_handle_line_longer_than_internal_buffer()
        {
            // The internal StreamReader buffer is 1024 bytes; the line is longer.
            var longLine = new string('x', 5000);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(longLine + "\nsecond"));
            using var reader = new StreamTextReader(stream, new UTF8Encoding(false), true) as ITextReader;

            reader.PeekToNextLine().Should().Be(longLine);
            reader.PeekToNextLine().Should().Be(longLine); // stability of a repeated peek
            reader.ReadLine().Should().Be(longLine);
            ((char)reader.Read()).Should().Be('s');
        }

        [Fact]
        public void Dispose_with_leaveOpen_false_should_close_underlying_stream()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
            var reader = new StreamTextReader(stream) as ITextReader;

            reader.Dispose();

            Action action = () => stream.ReadByte();
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_with_leaveOpen_true_should_keep_stream_open()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
            var reader = new StreamTextReader(stream, Encoding.UTF8, true) as ITextReader;

            reader.Dispose();

            Action action = () => stream.ReadByte();
            action.Should().NotThrow();
        }

        [Fact]
        public void Constructor_with_null_stream_should_throw()
        {
            Action action = () => new StreamTextReader(null);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_twice_should_be_safe()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
            var reader = new StreamTextReader(stream) as ITextReader;

            Action action = () =>
            {
                reader.Dispose();
                reader.Dispose();
            };

            action.Should().NotThrow();
        }
    }
}