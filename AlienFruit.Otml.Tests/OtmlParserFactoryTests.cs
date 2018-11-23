using AlienFruit.Otml.Factories;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace AlienFruit.Otml.Tests
{
    public class OtmlParserFactoryTests
    {
        private readonly string testDataFile = TestContext.CurrentContext.TestDirectory + @"\OtmlTestDataANSI.py";

        [Test]
        public void Test1()
        {
            using (var stream = File.OpenRead(testDataFile))
            //using (var reader = new StreamTextReader(stream, Encoding.Default, true))
            {
                //var result = OtmlParserFactory.ParseDeclaration(reader);

                var parser = new OtmlParserFactory().GetParser(stream);

                var result1 = parser.Parse();

                var r = new OtmlUnparserFactory().GetDefaultUnparser().Unparse(result1);
            }

            var otmlFactory = new OtmlUnparserFactory();
            var otmlUnparser = otmlFactory.GetDefaultUnparser();
            var otmlNodeFactory = otmlUnparser.GetNodeFactory();

            var dom = new[]
            {
                otmlNodeFactory.CreateNode(NodeType.Object, "testObject", new []
                {
                    otmlNodeFactory.CreateNode(NodeType.Property, "testProperty", new[]
                    {
                        otmlNodeFactory.CreateValue("test value")
                    })
                })
            };

            //unparce to stream
            using (var stream = File.OpenWrite(@"writeTest.otml"))
            {
                otmlUnparser.Unparse(dom, stream);
            }

            //unparce to string
            var result = otmlUnparser.Unparse(dom);
        }

        [Test]
        public void Test2()
        {
            var text = File.ReadAllText(testDataFile);

            var parser = new OtmlParserFactory(Encoding.UTF8).GetParser(text);
        }
    }
}