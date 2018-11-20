using AlienFruit.Otml.Factories;
using AlienFruit.Otml.Readers;
using NUnit.Framework;
using System.IO;
using System.Text;

namespace AlienFruit.Otml.Tests
{
    public class OtmlParserFactoryTests
    {
        private readonly string testDataFile = TestContext.CurrentContext.TestDirectory + @"\OtmlTestData.py";

        [Test]
        public void Test1()
        {
            using (var stream = File.OpenRead(testDataFile))
            using (var reader = new StreamTextReader(stream, Encoding.Default, true))
            {
                //var result = OtmlParserFactory.ParseDeclaration(reader);

                var parser = new OtmlParserFactory(Encoding.UTF8).GetParser(stream, true);
            }
        }

        [Test]
        public void Test2()
        {
            var text = File.ReadAllText(testDataFile);

            var parser = new OtmlParserFactory(Encoding.UTF8).GetParser(text);
        }
    }
}