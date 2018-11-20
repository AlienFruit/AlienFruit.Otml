using AlienFruit.Otml.Factories;
using AlienFruit.Otml.Serializer.Containers;
using AlienFruit.Otml.Serializer.Utils;
using System;
using System.IO;
using System.Text;

namespace AlienFruit.Otml.Serializer
{
    public class Serializer : ISerializer
    {
        private IResolver resolver;
        private Encoding encoding;
        private IParserFactory parserFactory;
        private IUnparserFactory unparserFactory;
        private Version parserVersion;

        public Serializer()
        {
        }

        public Serializer(Encoding encoding, IResolver resolver, IParserFactory factory, IUnparserFactory unparserFactory)
        {
            this.encoding = encoding;
            this.resolver = resolver;
            this.parserFactory = factory;
            this.unparserFactory = unparserFactory;
        }

        public Serializer(Encoding encoding, Version version, IResolver resolver, IParserFactory factory, IUnparserFactory unparserFactory)
        {
            this.encoding = encoding;
            this.parserVersion = version;
            this.resolver = resolver;
            this.parserFactory = factory;
            this.unparserFactory = unparserFactory;
        }

        #region Public method

        public T Deserialize<T>(string value) => (T)Deserialize(typeof(T), value);

        public object Deserialize(Type resultObjectType, string value)
        {
            var parser = this.parserFactory.ThrowIfNull("Parser and parser factory was not initialized").GetParser(value);
            return this.resolver.ThrowIfNull("Resolver was not initialized").GetFormatter(resultObjectType).DeserializeObject(parser.Parse());
        }

        public T Deserialize<T>(Stream stream, bool leaveOpen) => (T)Deserialize(typeof(T), stream, leaveOpen);

        public object Deserialize(Type resultObjectType, Stream stream, bool leaveOpen)
        {
            var parser = this.parserFactory.ThrowIfNull("Parser and parser factory was not initialized").GetParser(stream, leaveOpen);
            var result = this.resolver.ThrowIfNull("Resolver was not initialized")
                .GetFormatter(resultObjectType).DeserializeObject(parser.Parse());
            if (!leaveOpen)
                stream.Dispose();
            return result;
        }

        public string Serialize<T>(T value) => Serialize(typeof(T), value);

        public string Serialize(Type valueType, object value)
        {
            var unparser = GetUnparser();
            var treeModel = this.resolver.GetFormatter(valueType).SerializeObject(value, unparser.GetNodeFactory());
            return unparser.Unparse(treeModel);
        }

        public void Serialize<T>(T value, Stream stream, bool leaveOpen) => Serialize(typeof(T), value, stream, leaveOpen);

        public void Serialize(Type valueType, object value, Stream stream, bool leaveOpen)
        {
            var unparser = GetUnparser();
            var tree = this.resolver.GetFormatter(valueType).SerializeObject(value, unparser.GetNodeFactory());
            unparser.Unparse(tree, stream, leaveOpen);
        }

        #endregion Public method

        public static ISerializer Create() => Build().Create();

        public static SerializerBuilder Build() => new SerializerBuilder(new Serializer());

        private IUnparser GetUnparser()
        {
            var factory = this.unparserFactory.ThrowIfNull("Parser and parser factory was not initialized");
            var encoding = this.encoding.ThrowIfNull("Encoding was not initialized");
            return parserVersion is null ? factory.GetDefaultUnparser(encoding) : factory.GetUnparser(parserVersion, encoding);
        }

        public class SerializerBuilder
        {
            private readonly Serializer serializer;

            private IResolver resolver;
            private Encoding encoding;
            private ResolverContainer customContainer;
            private IParserFactory parserFactory;
            private IUnparserFactory unparserFactory;
            private Version version;

            public SerializerBuilder(Serializer serializer) => this.serializer = serializer;

            public SerializerBuilder WithEncoding(Encoding encoding)
            {
                this.encoding = encoding;
                return this;
            }

            public SerializerBuilder WithContainer(ResolverContainer container)
            {
                this.customContainer = container;
                return this;
            }

            public SerializerBuilder WithResolver(IResolver resolver)
            {
                this.resolver = resolver;
                return this;
            }

            public SerializerBuilder WithParserFactory(IParserFactory factory)
            {
                this.parserFactory = factory;
                return this;
            }

            public SerializerBuilder WithUnparserFactory(IUnparserFactory factory)
            {
                this.unparserFactory = factory;
                return this;
            }

            public SerializerBuilder WihParserVersion(Version version)
            {
                this.version = version;
                return this;
            }

            public ISerializer Create()
            {
                var serializer = this.serializer;
                serializer.encoding = this.encoding ?? Encoding.UTF8;
                serializer.parserVersion = this.version ?? new Version(1, 0);

                serializer.resolver = this.resolver
                    ?? new Resolver(new BaseFormattersContainer()
                    .Merge(new BaseObjectsContainer()
                    .Merge(this.customContainer)));
                serializer.parserFactory = this.parserFactory ?? new OtmlParserFactory(serializer.encoding);
                serializer.unparserFactory = this.unparserFactory ?? new OtmlUnparserFactory(serializer.encoding);
                return serializer;
            }
        }
    }
}