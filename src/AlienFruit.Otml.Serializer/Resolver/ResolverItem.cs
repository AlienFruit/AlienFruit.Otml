using System;

namespace AlienFruit.Otml.Serializer
{
    public delegate IFormatter FormatterFactory(Type formatterType, Type valueType, object[] constructorParams);

    internal class FormattingRule
    {
        public object Tag { get; }
        public Type FormatterType { get; }
        public Func<Type, bool> Predicate { get; }
        public FormatterFactory Factory { get; }

        public FormattingRule(Type formatterType, Func<Type, bool> predicate, FormatterFactory instanceCreator, object tag = null)
        {
            this.FormatterType = formatterType;
            this.Tag = tag;
            this.Predicate = predicate;
            this.Factory = instanceCreator;
        }
    }

    internal class InjectionRule
    {
        public Type Type { get; }
        public Func<IResolver, object> InstanceCreator { get; }

        public InjectionRule(Type type, Func<IResolver, object> instanceCreator)
        {
            this.Type = type;
            this.InstanceCreator = instanceCreator;
        }
    }
}