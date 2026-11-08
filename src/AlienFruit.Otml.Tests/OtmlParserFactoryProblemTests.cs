using System;
using System.IO;
using System.Linq;
using System.Text;
using AlienFruit.Otml;
using AlienFruit.Otml.Domain.Version1v0;
using AlienFruit.Otml.Factories;
using AlienFruit.Otml.Models;
using AlienFruit.Otml.Readers;
using FluentAssertions;
using Xunit;

namespace AlienFruit.Otml.Tests
{
    // Tests for known OtmlParserFactory / ParseDeclaration problems.
    // Asserts describe the correct expected behavior; failures pin the bugs.
    public class OtmlParserFactoryProblemTests
    {
        // =====================================================================
        // PROBLEM: the @@encoding declaration is parsed but ignored —
        // GetParser commented out the use of declaration.Encodind and
        // always decodes the stream with the default encoding. A UTF-16
        // document without BOM that honestly declares "@@encoding : utf-16" cannot be
        // read: UTF-16 bytes decoded as UTF-8 give garbage, the declaration is unrecognized.
        // =====================================================================

        [Fact]
        public void GetParser_should_respect_encoding_declaration_for_stream_without_bom()
        {
            // Arrange: UTF-16 LE without BOM, as prescribed by the declaration
            var text = "@@version : 1.0\n@@encoding : utf-16\np : v\n";
            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(text));
            var factory = new OtmlParserFactory();

            // Action
            using var parser = factory.GetParser(stream);

            // Assert
            var result = parser.Parse().ToList();
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("p");
        }

        // =====================================================================
        // Regression guards: behavior is currently correct.
        // =====================================================================

        [Fact]
        public void GetParser_should_throw_for_unknown_declaration_name()
        {
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@bogus : 1\nx : 1\n").Dispose();
            action.Should().Throw<ArgumentException>().WithMessage("Unknown*OTML declaration in line 1");
        }

        [Fact]
        public void GetParser_should_throw_for_unparsable_version()
        {
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@version : not-a-version\nx : 1\n").Dispose();
            action.Should().Throw<ArgumentException>().WithMessage("Cannot parse version*line 1");
        }

        [Fact]
        public void GetParser_should_throw_for_unparsable_encoding_name()
        {
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@version : 1.0\n@@encoding : bogus-cp\nx : 1\n").Dispose();
            action.Should().Throw<ArgumentException>().WithMessage("Cannot parse encoding*line 2");
        }

        [Fact]
        public void GetParser_should_throw_for_declaration_without_colon()
        {
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@version\nx : 1\n").Dispose();
            action.Should().Throw<ArgumentException>().WithMessage("Invalid*OTML declaration in line 1");
        }

        [Fact]
        public void GetParser_should_throw_for_version_without_value()
        {
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@version : \nx : 1\n").Dispose();
            action.Should().Throw<ArgumentException>().WithMessage("Cannot parse version*");
        }

        [Fact]
        public void GetParser_should_throw_for_third_colon_in_declaration()
        {
            // Arrange: "@@version : 1.0 : 2" — three parts after splitting on ':'
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@version : 1.0 : 2\nx : 1\n").Dispose();
            action.Should().Throw<ArgumentException>().WithMessage("Invalid*OTML declaration in line 1");
        }

        [Fact]
        public void GetParser_should_use_latest_parser_for_document_without_declaration()
        {
            var factory = new OtmlParserFactory();
            using var parser = factory.GetParser("p : v\n");
            parser.Parse().ToList().Should().HaveCount(1);
        }

        [Fact]
        public void GetParser_should_use_latest_parser_for_document_with_other_version()
        {
            // Arrange: version not from the registry (2.0) — fails at the parser selection stage
            var factory = new OtmlParserFactory();
            Action action = () => factory.GetParser("@@version : 2.0\nx : 1\n").Dispose();
            action.Should().Throw<InvalidOperationException>().WithMessage("A parser for version 2.0 was not found*");
        }

        [Fact]
        public void GetParser_should_accept_document_with_version_1_0()
        {
            var factory = new OtmlParserFactory();
            using var parser = factory.GetParser("@@version : 1.0\np : v\n");
            parser.Parse().ToList().Should().HaveCount(1);
        }

        [Fact]
        public void GetParser_should_accept_equivalent_version_notation()
        {
            // PROBLEM: Version("1.0.0") != Version("1.0") (Revision -1 vs 0),
            // so a document declaring "1.0.0" — the same format version —
            // is rejected as unknown.
            var factory = new OtmlParserFactory();
            using var parser = factory.GetParser("@@version : 1.0.0\np : v\n");
            parser.Parse().ToList().Should().HaveCount(1);
        }

        [Fact]
        public void GetParser_should_be_case_insensitive_for_declaration_names()
        {
            var factory = new OtmlParserFactory();
            using var parser = factory.GetParser("@@VERSION : 1.0\np : v\n");
            parser.Parse().ToList().Should().HaveCount(1);
        }

        [Fact]
        public void GetParser_for_null_string_should_not_throw()
        {
            var factory = new OtmlParserFactory();
            Action action = () =>
            {
                using var parser = factory.GetParser((string)null);
                parser.Parse().Should().BeEmpty();
            };
            action.Should().NotThrow();
        }

        [Fact]
        public void GetParser_with_leaveOpen_should_keep_stream_readable()
        {
            var text = "@@version : 1.0\np : v\n";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var factory = new OtmlParserFactory();

            using (factory.GetParser(stream, leaveOpen: true)) { }

            stream.CanRead.Should().BeTrue();
        }

        [Fact]
        public void GetParser_should_skip_declaration_lines_from_document_tree()
        {
            // Arrange: the declaration must not end up in the node tree
            var factory = new OtmlParserFactory();
            using var parser = factory.GetParser("@@version : 1.0\n@@encoding : utf-8\np : v\n");

            // Assert
            var result = parser.Parse().ToList();
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("p");
        }
    }
}
