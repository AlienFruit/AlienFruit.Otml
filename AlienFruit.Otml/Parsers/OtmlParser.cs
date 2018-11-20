using AlienFruit.Otml.Exceptions;
using AlienFruit.Otml.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Parsers
{
    internal class OtmlParser : IParser
    {
        private static readonly char[] newLineList = { OtmlSyntax.CR, OtmlSyntax.LF };
        private static readonly List<char> quotesList = new List<char> { OtmlSyntax.SingleQuote, OtmlSyntax.DoubleQuote };
        private static readonly char[] needForShield = { OtmlSyntax.DoubleQuote, OtmlSyntax.SingleQuote };

        private readonly ITextReader reader;

        public OtmlParser(ITextReader reader) => this.reader = reader;

        public IEnumerable<INode> Parse() => Parse(this.reader);

        #region Private

        private static IEnumerable<INode> Parse(ITextReader reader)
        {
            var result = new List<INode>();
            var levels = new Dictionary<int, INode>();
            int lastLevel = 0;

            int tabCounter = 0;
            int currentByte = 0;

            reader.OnNewLine += (x, y) => tabCounter = 0;

            while ((currentByte = reader.Read()) >= 0)
            {
                var currentChar = (char)currentByte;
                if (currentChar == OtmlSyntax.CommentChar) //skip line comment
                    reader.ToLineEnd();
                else
                if (!OtmlSyntax.SpaceList.Contains(currentChar) && !newLineList.Contains(currentChar)) //parse object/property
                {
                    if (tabCounter - lastLevel > 1)
                        throw new OtmlParseException($"Unexpected node level. Excess tabs count: {tabCounter - lastLevel - 1}", reader.CurrentLocation);
                    var newNode = currentChar == OtmlSyntax.ObjectChar
                        ? ParseItem(reader, true)
                        : ParseItem(reader, false, currentChar);

                    if (newNode.Name.FirstOrDefault() != OtmlSyntax.ObjectChar) //skip declaration object which start with @@
                    {
                        AddItem(newNode, result, levels, tabCounter, reader.CurrentLocation);
                        levels[tabCounter] = newNode;
                    }
                    lastLevel = tabCounter;
                    tabCounter = 0;
                }
                else
                if (currentChar == OtmlSyntax.TabChar)
                    tabCounter++;
                else
                    if (currentChar == OtmlSyntax.SpaceChar)
                    throw new OtmlParseException("Unacceptable space character, should use only a tab character in this plase", reader.CurrentLocation);
            }

            return result;
        }

        private static INode ParseItem(ITextReader reader, bool isObject, char? lostChar = null)
        {
            var line = reader.ReadLine();
            if (lostChar == null)
                return ParseLine(line, isObject, reader.CurrentLocation);
            else
                return ParseLine(lostChar + line, isObject, new CurrentCharLocation(reader.CurrentLocation.Line, reader.CurrentLocation.Position - 1));
        }

        private static INode ParseLine(string line, bool isObject, CurrentCharLocation location)
        {
            var property = GetNameAndValues(line);
            var offset = property.Item2 is null ? 0 : property.Item1.Length + 1 /*':'*/;
            var valueNodes = SplitInlineProperies(property.Item2 ?? property.Item1)
                .Select(x => new ValueNode(ParsePropertyValue(x, GetRelativeLocation(location, offset), out var hasPlus, out offset), hasPlus));

            if (property.Item2 is null && !isObject)
            {
                if (valueNodes.Count() > 1)
                    throw new OtmlParseException("Only one value can be in the line without a property", location);
                return valueNodes.First();
            }

            var propertyName = property.Item1.Trim();

            INode parrent = isObject
                ? new ObjectNode(propertyName) as INode
                : new PropertyNode(propertyName);
            AddToParrent(parrent, property.Item2 is null ? Enumerable.Empty<INode>() : valueNodes, location);
            return parrent;

            CurrentCharLocation GetRelativeLocation(CurrentCharLocation loc, int ofst) => new CurrentCharLocation(location.Line, location.Position + ofst);
        }

        private static void AddItem(INode item, List<INode> toList, Dictionary<int, INode> levels, int tabCounter, CurrentCharLocation location)
        {
            if (tabCounter == 0)
                toList.Add(item);
            else
            if (levels.ContainsKey(tabCounter - 1))
                AddToParrent(levels[tabCounter - 1], item, location);
            else
                throw new OtmlParseException($"Failed to recognoze parrents for item \'{(item.Type == NodeType.Value ? item.Value : item.Name)}\'", location);
        }

        private static void AddToParrent(INode parrent, INode item, CurrentCharLocation location)
        {
            #region Validation

            if (parrent.Type == NodeType.Value)
                throw new OtmlParseException("Value cannot has any properties", location);

            #endregion Validation

            var lastNode = parrent.Children.LastOrDefault();
            if (lastNode != null && lastNode.Type == NodeType.Value && item.Type == NodeType.Value && (lastNode as ValueNode).IsMergeable)
                (lastNode as ValueNode).Merge(item as ValueNode);
            else
                parrent.AddChild(item);
        }

        private static void AddToParrent(INode parrent, IEnumerable<INode> items, CurrentCharLocation location)
        {
            foreach (var item in items)
                AddToParrent(parrent, item, location);
        }

        internal static string ParsePropertyValue(string text, CurrentCharLocation location, out bool hasPlus, out int posOffset)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new OtmlParseException($"Empty property cannot be parsed", location.Line, location.Position);

            var result = new List<char>();
            var isShielded = false;
            hasPlus = IsPropertyHasPlus(text);

            var property = TrimProperty(text, location, out var trimOffset, out var trimmedQuotes);
            int inlinePos = 0;
            posOffset = 0;
            foreach (var currChar in property)
            {
                if (isShielded && !needForShield.Contains(currChar))
                    result.Add(OtmlSyntax.ShieldChar);

                var quoteIndex = quotesList.IndexOf(currChar);

                if (quoteIndex >= 0 && !isShielded && (trimmedQuotes is null || trimmedQuotes == currChar))
                    throw new OtmlParseException($"Unshielded quotation mark char ({quotesList[quoteIndex]})", location.Line, location.Position + trimOffset + inlinePos);

                if (currChar == OtmlSyntax.ShieldChar)
                    isShielded = true;
                else
                {
                    result.Add(currChar);
                    isShielded = false;
                }
                inlinePos++;
            }

            if (isShielded) //if slash was in last pos
                result.Add(OtmlSyntax.ShieldChar);

            posOffset = location.Position + text.Length;
            return new string(result.ToArray());
        }

        internal static bool IsPropertyHasPlus(string property)
            => property.TrimEnd().LastOrDefault() == OtmlSyntax.MultilineChar;

        internal static string TrimProperty(string property, CurrentCharLocation location, out int posOffset, out char? trimmedQuotes)
        {
            trimmedQuotes = null;
            var result = property.TrimStart(OtmlSyntax.SpaceList);

            var startQuoteIndex = quotesList.IndexOf(result.FirstOrDefault());
            if (startQuoteIndex >= 0)
                result = result.Substring(1);

            posOffset = property.Length - result.Length;

            result = result.TrimEnd(new[] { OtmlSyntax.MultilineChar }.Concat(OtmlSyntax.SpaceList).ToArray());

            var endQuoteIndex = quotesList.IndexOf(result.LastOrDefault());

            if (startQuoteIndex >= 0)
                if (startQuoteIndex != endQuoteIndex)
                    throw new OtmlParseException($"Missing closing quotation mark ({quotesList[startQuoteIndex]})", location.Line, location.Position + property.Length);
                else
                {
                    trimmedQuotes = quotesList[startQuoteIndex];
                    result = result.Remove(result.Length - 1);
                }
            return result;
        }

        /// <summary>
        /// Returns property name and property value/values without comments
        /// </summary>
        /// <param name="text">property line text</param>
        /// <returns></returns>
        internal static Tuple<string, string> GetNameAndValues(string text)
        {
            string result = null;
            var buffer = new List<char>();
            int openedQuotationMark = -1;
            var isShielded = false;
            for (int a = 0; a < text.Length; a++)
            {
                if (!isShielded)
                {
                    var findedQuotationMark = quotesList.IndexOf(text[a]);
                    if (findedQuotationMark >= 0)
                    {
                        if (findedQuotationMark == openedQuotationMark)
                            openedQuotationMark = -1;
                        else
                        if (openedQuotationMark < 0)
                            openedQuotationMark = findedQuotationMark;
                    }
                }

                if (text[a] == OtmlSyntax.CommentChar && openedQuotationMark < 0)
                    break;

                if (text[a] == OtmlSyntax.SplitChar && result is null && openedQuotationMark < 0)
                {
                    result = ReadBuff();
                    buffer.Clear();
                }
                else
                    buffer.Add(text[a]);

                isShielded = (text[a] == OtmlSyntax.ShieldChar) && !isShielded;
            }
            return result is null
                ? new Tuple<string, string>(ReadBuff(), null)
                : new Tuple<string, string>(result, ReadBuff());

            string ReadBuff() => new string(buffer.ToArray());
        }

        internal static string[] SplitInlineProperies(string text)
        {
            var result = new List<string>();
            var buffer = new List<char>();
            int openedQuotationMark = -1;
            var isShielded = false;
            for (int a = 0; a < text.Length; a++)
            {
                if (!isShielded)
                {
                    var findedQuotationMark = quotesList.IndexOf(text[a]);
                    if (findedQuotationMark >= 0)
                    {
                        if (findedQuotationMark == openedQuotationMark)
                            openedQuotationMark = -1;
                        else
                        if (openedQuotationMark < 0)
                            openedQuotationMark = findedQuotationMark;
                    }
                }

                if (text[a] == OtmlSyntax.PropsListSeparator && openedQuotationMark < 0)
                {
                    result.Add(new string(buffer.ToArray()));
                    buffer.Clear();
                }
                else
                    buffer.Add(text[a]);
                isShielded = (text[a] == OtmlSyntax.ShieldChar) && !isShielded;
            }
            var prop = new string(buffer.ToArray());
            if (!string.IsNullOrWhiteSpace(prop))
                result.Add(prop);
            return result.ToArray();
        }

        #endregion Private
    }
}