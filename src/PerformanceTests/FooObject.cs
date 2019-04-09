using System;
using System.Collections.Generic;

namespace PerformanceTests
{
    public class FooObject
    {
        public long TemplateId { get; set; }

        //public Foo2 FooValue { get; set; }
        public decimal Amount { get; set; }

        public string[] Comments { get; set; }
        public DateTime CrDate { get; set; }
        public bool Deleted { get; set; }
        public long DistributorId { get; set; }
        public long? BusinessStatusId { get; set; }
        public bool IsEditOut { get; set; }
        public DateTime OpDate { get; set; }
        public string Outercode { get; set; }
        public long PDADocNum { get; set; }
        public IEnumerable<Item> Items { get; set; }

        public class Item
        {
            public long Id { get; set; }
            public IEnumerable<ItemValue> Values { get; set; }
        }

        public class ItemPhoto
        {
            public long Id { get; set; }
            public string PhotoFileName { get; set; }
            public DateTime PhotoTime { get; set; }
        }

        public class ItemValue
        {
            public DateTime AnswerDate { get; set; }
            public decimal AnswerNumber { get; set; }
            public string AnswerStr { get; set; }
            public long Id { get; set; }
        }

        public class Foo2
        {
            public string Value1 { get; set; }
            public int Value2 { get; set; }
            public int[] Values { get; set; }

            public int? NullableValue { get; set; }

            public InnerClass InnerClass123 { get; set; }

            public InnerClass[] Values2 { get; set; }

            public class InnerClass
            {
                public string Value { get; set; }
            }
        }
    }
}