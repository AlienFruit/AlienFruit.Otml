using System;
using System.Collections.Generic;

namespace PerformanceTests
{
    public class TestObject
    {
        public long RouteId { get; set; }
        public long TemplateId { get; set; }
        public long Id { get; set; }
        public TestClass Test { get; set; }
        public decimal Amount { get; set; }
        public string[] Comment { get; set; }
        public DateTime CrDate { get; set; }
        public DateTime DateTimeStamp { get; set; }
        public bool Deleted { get; set; }
        public long DistributorId { get; set; }
        public long ActionId { get; set; }
        public long BaseDocId { get; set; }
        public long? BusinessStatusId { get; set; }
        public long OutletId { get; set; }
        public long ClientId { get; set; }
        public long CreatorId { get; set; }
        public long? ExchangeStateId { get; set; }
        public long FirstVersionId { get; set; }
        public long OperationId { get; set; }
        public long PhysicalPersonId { get; set; }
        public long PositionId { get; set; }
        public long PreviousVersionId { get; set; }
        public long StatusId { get; set; }
        public long IdvDJGridFields { get; set; }
        public long VisitId { get; set; }
        public bool IsEditOut { get; set; }
        public bool? IsReserved { get; set; }
        public bool IsSent { get; set; }
        public int? ObjectType { get; set; }
        public DateTime OpDate { get; set; }
        public string Outercode { get; set; }
        public long PDADocNum { get; set; }
        public string PrintDocNum { get; set; }
        public string PrnDocNum { get; set; }
        public string StatusSentMail { get; set; }
        public IEnumerable<Item> Items { get; set; }

        public class Item
        {
            public long Id { get; set; }
            public long IdDoc { get; set; }
            public long IdPacket { get; set; }
            public long IdQuestion { get; set; }
            public long IdTemplateRow { get; set; }
            public long IdTopic { get; set; }
            public IEnumerable<ItemPhoto> Photos { get; set; }
            public IEnumerable<ItemValue> Values { get; set; }
        }

        public class ItemPhoto
        {
            public long Id { get; set; }
            public long IdDocRow { get; set; }
            public long IdDoc { get; set; }
            public long IdQuestion { get; set; }
            public long IdTemplateRow { get; set; }
            public long IdTemplate { get; set; }
            public long IdTopic { get; set; }
            public string PhotoFileName { get; set; }
            public DateTime PhotoTime { get; set; }
        }

        public class ItemValue
        {
            public DateTime AnswerDate { get; set; }
            public long AnswerId { get; set; }
            public decimal AnswerNumber { get; set; }
            public string AnswerStr { get; set; }
            public long Id { get; set; }
            public long IdDocRow { get; set; }
            public long IdDoc { get; set; }
            public long IdQuestion { get; set; }
            public long IdTemplateRow { get; set; }
            public long IdTemplate { get; set; }
            public long IdTopic { get; set; }
        }

        public class TestClass
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
                //public Dictionary<int, string> Collection { get; set; }
            }
        }
    }
}