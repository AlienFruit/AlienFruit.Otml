using AlienFruit.Otml.Serializer.Formatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace AlienFruit.Otml.Serializer.Containers
{
    internal class BaseFormattersContainer : ResolverContainer
    {
        protected override void Init()
        {
            RegisterFormatter(typeof(PrimitiveFormatter<>),
                x => x.IsPrimitive || x == typeof(decimal),
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter);

            RegisterFormatter<StringFormatter>(
                x => x == typeof(string),
                (f, v, args) => Activator.CreateInstance(f, args) as IFormatter);

            RegisterFormatter(typeof(EnumFormatter<>),
                x => x.IsEnum,
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter);

            RegisterFormatter(typeof(NullableFormatter<>),
                x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>),
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v.GetTypeInfo().GetGenericArguments()), args) as IFormatter);

            RegisterFormatter<DateTimeFormatter>(
                x => x == typeof(DateTime),
                (f, v, args) => Activator.CreateInstance(f, args) as IFormatter);

            RegisterFormatter<UriFormatter>(
                x => x == typeof(Uri),
                (f, v, args) => Activator.CreateInstance(f, args) as IFormatter);

            RegisterFormatter<ColorFormatter>(
                x => x == typeof(Color),
                (f, v, args) => Activator.CreateInstance(f, args) as IFormatter);

            RegisterFormatter(typeof(ArrayFormatter<>), x => x.IsArray,
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v.GetElementType()), args) as IFormatter);

            RegisterFormatter(typeof(EnumerableFormatter<>),
                x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>),
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v.GetTypeInfo().GetGenericArguments()), args) as IFormatter);

            RegisterFormatter(typeof(ListFormatter<>),
                x => x.GetTypeInfo().IsGenericType && (
                x.GetGenericTypeDefinition() == typeof(List<>) ||
                x.GetGenericTypeDefinition() == typeof(IList<>) ||
                x.GetGenericTypeDefinition() == typeof(ICollection<>)),
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v.GetTypeInfo().GetGenericArguments()), args) as IFormatter);

            RegisterFormatter(typeof(DictionaryFotmatter<,>),
                x => x.GetTypeInfo().IsGenericType && (
                x.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                x.GetGenericTypeDefinition() == typeof(IDictionary<,>)),
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v.GetTypeInfo().GetGenericArguments()), args) as IFormatter);

            RegisterFormatter(typeof(ClassFormatter<>),
                x => x.IsClass && x.Namespace != nameof(System) && !x.IsArray && !x.IsGenericType,
                (f, v, args) => Activator.CreateInstance(f.MakeGenericType(v), args) as IFormatter);

            RegisterFormatter<ObjectFormatter>(x => x == typeof(object), (f, v, args) => Activator.CreateInstance(f, args) as IFormatter);
        }
    }
}