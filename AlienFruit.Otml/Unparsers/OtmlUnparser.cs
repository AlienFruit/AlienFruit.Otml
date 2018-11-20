﻿using AlienFruit.Otml.Factories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AlienFruit.Otml.Unparsers
{
    internal class OtmlUnparser : IUnparser
    {
        private static readonly char[] needForQuotes = { OtmlSyntax.CommentChar, OtmlSyntax.MultilineChar, OtmlSyntax.PropsListSeparator, OtmlSyntax.DoubleQuote, OtmlSyntax.SingleQuote };

        private readonly Encoding encoding;

        public OtmlUnparser(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public string Unparse(IEnumerable<INode> tree)
        {
            var result = new StringBuilder();
            var level = 0;
            UnparseTree(tree, result, null, level);

            return result.ToString();
        }

        public void Unparse(IEnumerable<INode> tree, Stream toStream, bool leaveOpen)
        {
            using (var writer = new StreamWriter(toStream, this.encoding, 1024, leaveOpen))
            {
                int level = 0;
                UnparseTree(tree, writer, null, level);
            }
        }

        public INodeFactory GetNodeFactory() => new OtmlNodeFactory();

        #region private

        private static void UnparseTree(IEnumerable<INode> tree, StringBuilder builder, INode parrent, int level)
        {
            foreach (var item in tree)
            {
                if (item is null)
                    continue;
                if (item.Type == NodeType.Value && !item.IsMultiline && parrent != null && (tree.Count() == 1 && parrent.Type == NodeType.Property || parrent.Type == NodeType.Object))
                    continue;

                builder.AppendLine(ItemToString(item, level));
                if (item.Type != NodeType.Value)
                    UnparseTree(item.Children, builder, item, level + 1);
            }
        }

        private static void UnparseTree(IEnumerable<INode> tree, StreamWriter writer, INode parrent, int level)
        {
            foreach (var item in tree)
            {
                if (item is null)
                    continue;
                if (item.Type == NodeType.Value && !item.IsMultiline && parrent != null && (tree.Count() == 1 && parrent.Type == NodeType.Property || parrent.Type == NodeType.Object))
                    continue;

                writer.WriteLine(ItemToString(item, level));
                if (item.Type != NodeType.Value)
                    UnparseTree(item.Children, writer, item, level + 1);
            }
        }

        private static string ItemToString(INode item, int level)
        {
            var result = new StringBuilder(CreateTabs(level));
            if (item.Type == NodeType.Object)
                result.Append(OtmlSyntax.ObjectChar);

            switch (item.Type)
            {
                case NodeType.Value:
                    if (item.IsMultiline)
                    {
                        var lines = item.Value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        result.AppendFormat("\"{0}\"{1}", lines[0], OtmlSyntax.MultilineChar);
                        for (int a = 1; a < lines.Length; a++)
                        {
                            result.AppendFormat("{0}{1}\"{2}\"", Environment.NewLine, CreateTabs(level), lines[a]);
                            if (a < lines.Length - 1)
                                result.Append(OtmlSyntax.MultilineChar);
                        }
                    }
                    else
                        result.Append(ShieldValue(item.Value));
                    break;

                case NodeType.Object:
                    result.Append(item.Name + (item.Children.Any(x => x.Type == NodeType.Value) ? $" {OtmlSyntax.SplitChar} " : null));
                    var values = item.Children.Where(x => x.Type == NodeType.Value && !x.IsMultiline);
                    if (values.Any())
                    {
                        result.Append(string.Join(OtmlSyntax.PropsListSeparator.ToString() + ' ', values.Select(x => ShieldValue(x.Value))));
                    }
                    break;

                case NodeType.Property:
                    result.Append(item.Name + $" {OtmlSyntax.SplitChar} ");
                    if (!IsArrayProperty(item) && !IsMultilineProperty(item))
                        result.Append(ShieldValue(item.Value));
                    break;
            }
            return result.ToString();

            bool IsArrayProperty(INode property) => property.Children.Count() > 1;

            bool IsMultilineProperty(INode property)
                => property.Children.SingleOrDefault()?.IsMultiline ?? false;
        }

        private static string CreateTabs(int tabsCount)
        {
            var result = new List<char>();
            for (int a = 0; a < tabsCount; a++)
                result.Add(OtmlSyntax.TabChar);
            return new string(result.ToArray());
        }

        private static string ShieldValue(string value)
        {
            var result = value
                //.Replace(shieldChar.ToString(), $"{shieldChar}{shieldChar}")
                .Replace(OtmlSyntax.DoubleQuote.ToString(), $"{OtmlSyntax.ShieldChar}{OtmlSyntax.DoubleQuote}")
                .Replace(OtmlSyntax.SingleQuote.ToString(), $"{OtmlSyntax.ShieldChar}{OtmlSyntax.SingleQuote}");
            return (result.Any(x => needForQuotes.Contains(x)) ||
                OtmlSyntax.SpaceList.Contains(result.FirstOrDefault()) ||
                OtmlSyntax.SpaceList.Contains(result.LastOrDefault()) ||
                OtmlSyntax.ObjectChar == result.FirstOrDefault())
                ? $"\"{result}\"" : result;
        }

        #endregion private
    }
}