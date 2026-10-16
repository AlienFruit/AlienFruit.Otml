namespace AlienFruit.Otml.Configuration.WebApplicationTest
{
    public class TestConfiguration
    {
        public static string Name => nameof(TestConfiguration);

        public required string TestString { get; set; }
    }
}
