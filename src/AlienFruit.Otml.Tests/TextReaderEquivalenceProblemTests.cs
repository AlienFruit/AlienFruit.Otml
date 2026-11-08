using System;
using System.IO;
using System.Text;
using AlienFruit.Otml.Readers;
using FluentAssertions;
using Xunit;

namespace AlienFruit.Otml.Tests
{
    // Tests for known problems of both readers as one pair of ITextReader
    // contract implementations. Asserts describe the correct expected behavior
    // (both readers must behave identically on identical data);
    // failures pin the divergences.
    public class TextReaderEquivalenceProblemTests
    {
        private static ITextReader CreateString(string text) => new StringTextReader(text);

        private static ITextReader CreateStream(string text)
        {
            // the stream stays open (leaveOpen); the test itself remains
            // the lifecycle owner; encoding without BOM — data is identical to the string one
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return new StreamTextReader(stream, new UTF8Encoding(false), true);
        }

        // =====================================================================
        // PROBLEM: use after Dispose. StreamTextReader returns
        // EOF (-1/null), StringTextReader — Dispose is stateless and
        // keeps serving the content. The same document reads
        // differently after being disposed.
        // =====================================================================

        [Fact]
        public void Read_after_dispose_should_return_eof_in_both_readers()
        {
            // Arrange
            var stringReader = CreateString("abc");
            var streamReader = CreateStream("abc");
            stringReader.Dispose();
            streamReader.Dispose();

            // Action / Assert: expected behavior — EOF in both
            streamReader.Read().Should().Be(-1);
            stringReader.Read().Should().Be(-1,
                "StringTextReader after Dispose should behave like StreamTextReader (EOF), not serve the content");
        }

        [Fact]
        public void ReadLine_after_dispose_should_return_null_in_both_readers()
        {
            // Arrange
            var stringReader = CreateString("abc");
            var streamReader = CreateStream("abc");
            stringReader.Dispose();
            streamReader.Dispose();

            // Action / Assert
            streamReader.ReadLine().Should().BeNull();
            stringReader.ReadLine().Should().BeNull();
        }

        [Fact]
        public void PeekToNextLine_after_dispose_should_return_empty_in_both_readers()
        {
            // Arrange
            var stringReader = CreateString("abc");
            var streamReader = CreateStream("abc");
            stringReader.Dispose();
            streamReader.Dispose();

            // Action / Assert
            streamReader.PeekToNextLine().Should().BeEmpty();
            stringReader.PeekToNextLine().Should().BeEmpty();
        }

        // =====================================================================
        // Regression guards: equivalence tests, currently passing.
        // =====================================================================

        [Fact]
        public void Both_readers_should_produce_identical_read_sequence()
        {
            var text = "line one\r\nвторая\r\tthree\n";
            var stringReader = CreateString(text);
            var streamReader = CreateStream(text);

            var stringChars = ReadAll(stringReader);
            var streamChars = ReadAll(streamReader);

            streamChars.Should().Be(stringChars);
        }

        [Fact]
        public void Both_readers_should_produce_identical_readline_sequence()
        {
            var text = "a\nb\r\nc\nd\n";
            var stringReader = CreateString(text);
            var streamReader = CreateStream(text);

            var stringLines = ReadAllLines(stringReader);
            var streamLines = ReadAllLines(streamReader);

            streamLines.Should().Equal(stringLines);
        }

        [Fact]
        public void Both_readers_should_produce_identical_location_after_full_read()
        {
            var text = "x\nyy\nzzz";
            var stringReader = CreateString(text);
            var streamReader = CreateStream(text);

            ReadAll(stringReader);
            ReadAll(streamReader);

            streamReader.CurrentLocation.Should().BeEquivalentTo(stringReader.CurrentLocation);
        }

        [Fact]
        public void Both_readers_should_strip_leading_bom()
        {
            var stringReader = CreateString("\uFEFFabc");
            var streamReader = CreateStream("\uFEFFabc");

            ((char)stringReader.Read()).Should().Be('a');
            ((char)streamReader.Read()).Should().Be('a');
        }

        private static string ReadAll(ITextReader reader)
        {
            var result = new StringBuilder();
            int c;
            while ((c = reader.Read()) >= 0)
                result.Append((char)c);
            return result.ToString();
        }

        private static string[] ReadAllLines(ITextReader reader)
        {
            var result = new System.Collections.Generic.List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
                result.Add(line);
            return result.ToArray();
        }
    }
}
