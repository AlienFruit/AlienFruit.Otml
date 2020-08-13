using AlienFruit.Otml.Readers;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace AlienFruit.Otml.Tests
{
    public class StreamTextReaderTests
    {
        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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
    }
}