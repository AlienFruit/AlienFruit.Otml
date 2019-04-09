using AlienFruit.Otml.Serializer.Utils;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AlienFruit.Otml.Serializer
{
    internal class Resolver : IResolver
    {
        private readonly ResolverContainer container;
        private readonly ConcurrentDictionary<Type, IFormatter> formattersCache;
        private readonly ConcurrentDictionary<Type, object> objectsCache;

        public Resolver(ResolverContainer container)
        {
            this.container = container.ThrowIfNull("Resolver container is not initialized");
            this.formattersCache = new ConcurrentDictionary<Type, IFormatter>();
            this.objectsCache = new ConcurrentDictionary<Type, object>();
        }

        public IFormatter<T> GetFormatter<T>() => (IFormatter<T>)GetFormatter(typeof(T));

        public IFormatter GetFormatter(Type type)
            => this.formattersCache.GetOrAdd(type, x => CreateFormatter(type));

        public T GetObject<T>()
            => (T)this.objectsCache.GetOrAdd(typeof(T), CreateObject(typeof(T)));

        private IFormatter CreateFormatter(Type valueType)
        {
            var customRules = this.container.CustomFormatterRules.Where(x => x.Predicate(valueType));//x.Type == valueType);
            if (customRules.Count() > 1)
                throw new InvalidOperationException($"Should be registered only one custom formatter for type \"{valueType}\"");
            if (customRules.Any())
                return GetFormatter(customRules.First());

            var rules = this.container.FormatterRules.Where(x => x.Predicate(valueType));
            if (rules.Count() > 1)
                throw new InvalidOperationException($"There are {rules.Count()} formatters registered for type \"{valueType}\", should be only one");
            if (!rules.Any())
                throw new InvalidOperationException($"Formatter was not found for type \"{valueType}\"");
            return GetFormatter(rules.First());

            IFormatter GetFormatter(FormattingRule rule) => rule.Factory.Invoke(rule.FormatterType, valueType, GetConstructorArguments(rule.FormatterType));
        }

        private object CreateObject(Type type)
        {
            var rules = this.container.InjectionRules.Where(x => x.Type == type);

            if (rules.Count() > 1)
                throw new InvalidOperationException($"There are {rules.Count()} registration for type \"{type}\", should be only one");
            if (!rules.Any())
                throw new InvalidOperationException($"Registration was not found for type \"{type}\"");

            return rules.First().InstanceCreator.Invoke(this);
        }

        private object[] GetConstructorArguments(Type type)
        {
            var ctors = type.GetConstructors();
            if (!ctors.Any())
                return Enumerable.Empty<object>().ToArray();
            if (ctors.Count() > 1)
                throw new InvalidOperationException($"Formatter {type} should have only one constructor");
            return ctors[0].GetParameters()
                .Select(x => objectsCache.GetOrAdd(x.ParameterType, CreateObject(x.ParameterType)))
                .ToArray();
        }
    }
}