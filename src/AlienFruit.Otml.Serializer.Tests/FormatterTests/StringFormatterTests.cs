using AlienFruit.Otml.Serializer.Formatters;
using Xunit;

namespace AlienFruit.Otml.Serializer.Tests.FormatterTests
{
    public class StringFormatterTests : BaseFormatterTest
    {
        [Fact]
        public void Serialize()
        {
            //var resolver = new StubResolver<string>(fixture);
            var formatter = new StringFormatter();
        }
    }
}