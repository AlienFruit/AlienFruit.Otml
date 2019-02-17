using AlienFruit.Otml.Serializer.Formatters;
using NUnit.Framework;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    [Parallelizable(ParallelScope.Children)]
    public class StringFormatterTests : BaseFormatterTest
    {
        [Test]
        public void Serialize()
        {
            //var resolver = new StubResolver<string>(fixture);
            var formatter = new StringFormatter();
        }
    }
}