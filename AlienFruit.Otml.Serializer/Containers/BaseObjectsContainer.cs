namespace AlienFruit.Otml.Serializer.Containers
{
    internal class BaseObjectsContainer : ResolverContainer
    {
        //private readonly IParser parser;
        //private readonly IParserFactory parserFactory;

        //public BaseObjectsContainer(IParserFactory parserFactory) => this.parserFactory = parserFactory;

        //public BaseObjectsContainer(IParser parser) => this.parser = parser;

        protected override void Init()
        {
            RegisterObject<IResolver>(x => x);
            //RegisterObject<INodeFactory>(x => this.parser.GetNodeFactory());
            //RegisterObject<IParser>(x => this.parser);
            //RegisterObject<IParserFactory>(x => parserFactory);
        }
    }
}