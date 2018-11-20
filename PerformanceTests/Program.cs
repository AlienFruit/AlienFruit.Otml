using AlienFruit.Otml.Serializer;
using AutoFixture;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace PerformanceTests
{
    internal class Program
    {
        private readonly static Fixture fixture = new Fixture();

        private readonly static string otmlInput = File.ReadAllText("TestObject.otml");
        private readonly static string jsonInput = File.ReadAllText("TestObject.json");
        private readonly static string yamlInput = File.ReadAllText("TestObject.yaml");

        private readonly static Dictionary<string, Func<object, string>> toStringConverters = new Dictionary<string, Func<object, string>>
        {
            { "AlienFruit.Otml", x => Serializer.Build().Create().Serialize(x) },
            { "Newtonsoft.Json", x => JsonConvert.SerializeObject(x, Formatting.Indented)},
            { "JavaScriptSerializer", x => new JavaScriptSerializer().Serialize(x) },
            { "YamlDotNet", x => new YamlDotNet.Serialization.Serializer().Serialize(x)},
        };

        private readonly static Dictionary<string, Func<object>> toObjectConverters = new Dictionary<string, Func<object>>
        {
            { "AlienFruit.Otml", () => Serializer.Build().Create().Deserialize<TestObject>(otmlInput) },
            { "Newtonsoft.Json", () => JsonConvert.DeserializeObject<TestObject>(jsonInput)},
            { "JavaScriptSerializer", () => new JavaScriptSerializer().Deserialize<TestObject>(jsonInput) },
            { "YamlDotNet", () => new YamlDotNet.Serialization.Deserializer().Deserialize(jsonInput, typeof(TestObject))},
        };

        private static void Main(string[] args)
        {
            SerializationSpeedTest(args.Contains("info"));
            DeserializationSpeedTest();
            Console.ReadKey();
        }

        private static void SerializationSpeedTest(bool showDetails)
        {
            for (int a = 0; a < 3; a++)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[serialization speed testing] case: {a}");
                var testTarget = fixture.Build<TestObject>()
                    .With(x => x.Outercode, "блаблаблаблаблаблабла")
                    .Create() as object;

                string result = string.Empty;

                foreach (var converter in toStringConverters)
                {
                    for (int b = 0; b < 10; b++)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[{converter.Key}]");
                        var watch = Stopwatch.StartNew();
                        result = converter.Value.Invoke(testTarget);
                        watch.Stop();
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($" stage {b} ellapsed elapsed milliseconds: {watch.ElapsedMilliseconds}, elapsed ticks: {watch.ElapsedTicks}");
                    }
                    Console.WriteLine();

                    File.WriteAllText($"D:\\test_{converter.Key}.txt", result);

                    if (showDetails)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(result);
                        Console.WriteLine();
                    }
                }
            }
        }

        private static void DeserializationSpeedTest()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[deserialization speed testing]");
            object result = null;
            foreach (var converter in toObjectConverters)
            {
                for (int b = 0; b < 10; b++)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"[{converter.Key}]");
                    var watch = Stopwatch.StartNew();
                    result = converter.Value.Invoke();
                    if (result is null) throw new Exception();
                    watch.Stop();
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($" stage {b} ellapsed elapsed milliseconds: {watch.ElapsedMilliseconds}, elapsed ticks: {watch.ElapsedTicks}");
                }
                Console.WriteLine();
            }
        }
    }
}