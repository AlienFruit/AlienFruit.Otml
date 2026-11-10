using AlienFruit.Otml.Exceptions;
using AlienFruit.Otml.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienFruit.Otml.Domain.Version1v0
{
    internal class OtmlParser : IParser
    {
        private static readonly char[] newLineList = { OtmlSyntax.CR, OtmlSyntax.LF };
        private static readonly char[] quotesList = { OtmlSyntax.SingleQuote, OtmlSyntax.DoubleQuote };
        private static readonly char[] needForShield = { OtmlSyntax.DoubleQuote, OtmlSyntax.SingleQuote };

        private readonly ITextReader reader;

        private bool disposed = false;

        public OtmlParser(ITextReader reader) => this.reader = reader;

        public IEnumerable<OtmlNode> Parse() => Parse(this.reader);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static IEnumerable<OtmlNode> Parse(ITextReader reader)
        {
            var result = new List<OtmlNode>();
            var levels = new Dictionary<int, OtmlNode>();
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
                    {
                        throw new OtmlParseException($"Unexpected node level. Excess tabs count: {tabCounter - lastLevel - 1}", reader.CurrentLocation);
                    }

                    // ReadLine consumes the line together with the '\n' newline, so
                    // OnNewLine resets tabCounter already inside ParseItem.
                    // The node level is captured before the call, otherwise nesting is lost.
                    var nodeLevel = tabCounter;

                    // Node coordinates are captured BEFORE ReadLine: after '\n' is consumed,
                    // CurrentLocation points at the next line (line +1, position 0/-1).
                    var nodeLocation = reader.CurrentLocation;

                    var newNode = currentChar == OtmlSyntax.ObjectChar
                        ? ParseItem(reader, true, nodeLocation)
                        : ParseItem(reader, false, nodeLocation, currentChar);

                    // The node text starts one character left of nodeLocation: that location
                    // was captured right after the first character of the node was read.
                    newNode.Location = new SourceLocation(nodeLocation.Line, nodeLocation.Position - 1);

                    if (newNode.Name.FirstOrDefault() != OtmlSyntax.ObjectChar) //skip declaration object which start with @@
                    {
                        AddItem(newNode, result, levels, nodeLevel, nodeLocation);
                        levels[nodeLevel] = newNode;
                    }
                    lastLevel = nodeLevel;
                    tabCounter = 0;
                }
                else
                if (currentChar == OtmlSyntax.TabChar)
                {
                    tabCounter++;
                }
                else
                if (currentChar == OtmlSyntax.SpaceChar)
                {
                    throw new OtmlParseException("Unacceptable space character, should use only a tab character in this plase", reader.CurrentLocation);
                }
            }

            return result;
        }

        private static OtmlNode ParseItem(ITextReader reader, bool isObject, SourceLocation nodeLocation, char? lostChar = null)
        {
            // ReadLine returns null only at EOF; here there is still a character before EOF,
            // but the line may be empty — keep the parser's previous behavior.
            var line = reader.ReadLine() ?? string.Empty;
            if (lostChar == null)
                return ParseLine(line, isObject, nodeLocation);
            else
                // nodeLocation was captured after reading lostChar: line start is one character to the left
                return ParseLine(lostChar + line, isObject, new SourceLocation(nodeLocation.Line, nodeLocation.Position - 1));
        }

        private static OtmlNode ParseLine(string line, bool isObject, SourceLocation location)
        {
            var property = GetNameAndValues(line);
            var offset = property.Item2 is null ? 0 : property.Item1.Length + 1 /*':'*/;
            var valueNodes = SplitInlineProperies(property.Item2 ?? property.Item1)
                .Select(x =>
                {
                    var value = ParsePropertyValue(x, GetRelativeLocation(location, offset), out var hasPlus, out offset);
                    return new ValueNode(value, hasPlus);
                });

            if (property.Item2 is null && !isObject)
            {
                if (valueNodes.Count() > 1)
                    throw new OtmlParseException("Only one value can be in the line without a property", location);
                return valueNodes.First();
            }

            // the name may be quoted (the unparser escapes ':'/'#'/' ' in names) — strip the quotes
            var propertyName = TrimName(property.Item1);

            OtmlNode parrent = isObject
                ? new ObjectNode(propertyName) as OtmlNode
                : new PropertyNode(propertyName, hasArraySyntax: string.IsNullOrWhiteSpace(property.Item2));
            AddToParrent(parrent, property.Item2 is null ? Enumerable.Empty<OtmlNode>() : valueNodes, location);
            return parrent;

            SourceLocation GetRelativeLocation(SourceLocation loc, int ofst) => new SourceLocation(location.Line, location.Position + ofst);
        }

        private static void AddItem(OtmlNode item, List<OtmlNode> toList, Dictionary<int, OtmlNode> levels, int tabCounter, SourceLocation location)
        {
            if (tabCounter == 0)
                toList.Add(item);
            else
            if (levels.ContainsKey(tabCounter - 1))
                AddToParrent(levels[tabCounter - 1], item, location);
            else
                throw new OtmlParseException($"Failed to recognoze parrents for item \'{(item.Type == NodeType.Value ? item.Value : item.Name)}\'", location);
        }

        private static void AddToParrent(OtmlNode parrent, OtmlNode item, SourceLocation location)
        {
            #region Validation

            if (parrent.Type == NodeType.Value)
                throw new OtmlParseException("Value cannot has any properties", location);

            #endregion Validation

            var lastNode = parrent.Children.LastOrDefault();
            if (lastNode != null && lastNode.Type == NodeType.Value && item.Type == NodeType.Value && (lastNode as ValueNode).IsMergeable)
                (lastNode as ValueNode).Merge(item as ValueNode);
            else
                (parrent as Node).AddChild(item);
        }

        private static void AddToParrent(OtmlNode parrent, IEnumerable<OtmlNode> items, SourceLocation location)
        {
            foreach (var item in items)
                AddToParrent(parrent, item, location);
        }

        internal static string ParsePropertyValue(string text, SourceLocation location, out bool hasPlus, out int posOffset)
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

                var quoteIndex = Array.IndexOf(quotesList, currChar);

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

        /// <summary>
        /// Strips surrounding quotes from a name (the unparser uses them to escape ':'/'#'/' ')
        /// and unescapes shield-character sequences, mirroring the value handling.
        /// Unlike values, unescaped quotes inside a name are not an error:
        /// the name is returned as is to avoid breaking parsing backward compatibility.
        /// </summary>
        internal static string TrimName(string name)
        {
            var result = name.Trim(OtmlSyntax.SpaceList);
            if (result.Length < 2 || !quotesList.Contains(result[0]) || result[^1] != result[0])
                return result;

            var inner = result.Substring(1, result.Length - 2);
            var unescaped = new List<char>();
            var isShielded = false;
            foreach (var currChar in inner)
            {
                if (isShielded && !quotesList.Contains(currChar))
                    unescaped.Add(OtmlSyntax.ShieldChar);

                if (currChar == OtmlSyntax.ShieldChar)
                    isShielded = true;
                else
                {
                    unescaped.Add(currChar);
                    isShielded = false;
                }
            }

            if (isShielded)
                unescaped.Add(OtmlSyntax.ShieldChar);

            return new string(unescaped.ToArray());
        }

        internal static string TrimProperty(string property, SourceLocation location, out int posOffset, out char? trimmedQuotes)
        {
            trimmedQuotes = null;
            var result = property.TrimStart(OtmlSyntax.SpaceList);

            var startQuoteIndex = Array.IndexOf(quotesList, result.FirstOrDefault());
            if (startQuoteIndex >= 0)
                result = result.Substring(1);

            posOffset = property.Length - result.Length;

            result = result.TrimEnd(new[] { OtmlSyntax.MultilineChar }.Concat(OtmlSyntax.SpaceList).ToArray());

            var endQuoteIndex = Array.IndexOf(quotesList, result.LastOrDefault());

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
                    var findedQuotationMark = Array.IndexOf(quotesList, text[a]);
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
                    var findedQuotationMark = Array.IndexOf(quotesList, text[a]);
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                this.reader.Dispose();

            disposed = true;
        }
    }
}