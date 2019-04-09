using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Serializer
{
    public abstract class ResolverContainer
    {
        private readonly List<FormattingRule> formattingRules = new List<FormattingRule>();
        private readonly List<FormattingRule> customFormattingRules = new List<FormattingRule>();
        private readonly List<InjectionRule> injectionRules = new List<InjectionRule>();

        internal IEnumerable<FormattingRule> FormatterRules => formattingRules;
        internal IEnumerable<FormattingRule> CustomFormatterRules => customFormattingRules;
        internal IEnumerable<InjectionRule> InjectionRules => injectionRules;

        public ResolverContainer() => Init();

        public ResolverContainer(ResolverContainer baseContainer)
        {
            this.formattingRules = baseContainer.FormatterRules.ToList();
            this.injectionRules = baseContainer.InjectionRules.ToList();
            Init();
        }

        public ResolverContainer Merge(ResolverContainer toContainer)
        {
            if (toContainer is null)
                return this;
            formattingRules.AddRange(toContainer.FormatterRules);
            customFormattingRules.AddRange(toContainer.CustomFormatterRules);
            injectionRules.AddRange(toContainer.InjectionRules);
            return this;
        }

        protected internal void RegisterFormatter<T>(Func<Type, bool> predicate, FormatterFactory instanceCreator, object tag = null)
            => formattingRules.Add(new FormattingRule(typeof(T), predicate, instanceCreator, tag));

        protected internal void RegisterFormatter(Type formatterType, Func<Type, bool> predicate, FormatterFactory instanceCreator, object tag = null)
            => formattingRules.Add(new FormattingRule(formatterType, predicate, instanceCreator, tag));

        protected void RegisterCustomFormatter(Type formatterType, Func<Type, bool> predicate, FormatterFactory formatterCreator, object tag = null)
            => customFormattingRules.Add(new FormattingRule(formatterType, predicate, formatterCreator, tag));

        protected void RegisterObject<T>(Func<IResolver, object> instanceCreator)
            => injectionRules.Add(new InjectionRule(typeof(T), instanceCreator));

        protected abstract void Init();
    }
}