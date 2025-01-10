using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Markup;
using System.Windows.Media;

namespace LightTextEditorPlus.Document
{
    /// <summary>
    /// 对<see cref="FontFamilyMap"/>的封装，提供了字符映射查找等功能
    /// 参考了PresentationCore.FamilyMap.cs中的大量代码
    /// </summary>
    internal class FontFamilyMapItem
    {
        internal const int LastUnicodeScalar = 0x10ffff;
        private Range[] _ranges;

        private FontFamilyMap Map { get; }

        public FontFamilyMapItem(FontFamilyMap map)
        {
            Map = map;
            _ranges = ParseUnicodeRanges(Map.Unicode);
        }

        /// <summary>
        /// Language to which the FontFamilyMap applies.
        /// </summary>
        /// <remarks>
        /// This property can be a specific language if the FontFamilyMap applies to just that 
        /// language, a neutral language if it applies to a group of related languages, or 
        /// the empty string if it applies to any language. The default value is the empty string.
        /// </remarks>
        public XmlLanguage Language
        {
            get => Map.Language;
            set => Map.Language = value;
        }

        /// <summary>
        /// Font scaling factor relative to EM
        /// </summary>
        public double Scale
        {
            get => Map.Scale;
            set => Map.Scale = value;
        }

        public string Unicode
        {
            set
            {
                Map.Unicode = value;
                _ranges = ParseUnicodeRanges(Map.Unicode);
            }

            get => Map.Unicode;
        }

        /// <summary>
        /// Target font family name in which the ranges map to
        /// </summary>
        public string Target
        {
            get => Map.Target;

            set => Map.Target = value;
        }

        internal Range[] Ranges => _ranges;

        private static void ThrowInvalidUnicodeRange()
        {
            throw new InvalidOperationException("无效的Unicode字符范围");
        }

        private static Range[] ParseUnicodeRanges(string unicodeRanges)
        {
            List<Range> ranges = new List<Range>(3);
            int index = 0;
            while (index < unicodeRanges.Length)
            {
                int firstNum;
                if (!ParseHexNumber(unicodeRanges, ref index, out firstNum))
                {
                    ThrowInvalidUnicodeRange();
                }

                int lastNum = firstNum;

                if (index < unicodeRanges.Length)
                {
                    if (unicodeRanges[index] == '?')
                    {
                        do
                        {
                            firstNum = firstNum * 16;
                            lastNum = lastNum * 16 + 0x0F;
                            index++;
                        } while (
                            index < unicodeRanges.Length &&
                            unicodeRanges[index] == '?' &&
                            lastNum <= LastUnicodeScalar);
                    }
                    else if (unicodeRanges[index] == '-')
                    {
                        index++; // pass '-' character
                        if (!ParseHexNumber(unicodeRanges, ref index, out lastNum))
                        {
                            ThrowInvalidUnicodeRange();
                        }
                    }
                }

                if (firstNum > lastNum ||
                    lastNum > LastUnicodeScalar ||
                    (index < unicodeRanges.Length && unicodeRanges[index] != ','))
                {
                    ThrowInvalidUnicodeRange();
                }

                ranges.Add(new Range(firstNum, lastNum));

                index++; // ranges seperator comma
            }

            return ranges.ToArray();
        }

        /// <summary>
        /// helper method to convert a string (written as hex number) into number.
        /// </summary>
        internal static bool ParseHexNumber(string numString, ref int index, out int number)
        {
            while (index < numString.Length && numString[index] == ' ')
            {
                index++;
            }

            var startIndex = index;

            number = 0;

            while (index < numString.Length)
            {
                var n = (int) numString[index];
                if (n >= (int) '0' && n <= (int) '9')
                {
                    number = (number * 0x10) + (n - ((int) '0'));
                    index++;
                }
                else
                {
                    n |= 0x20; // [A-F] --> [a-f]
                    if (n >= (int) 'a' && n <= (int) 'f')
                    {
                        number = (number * 0x10) + (n - ((int) 'a' - 10));
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            bool retValue = index > startIndex;

            while (index < numString.Length && numString[index] == ' ')
            {
                index++;
            }

            return retValue;
        }


        internal bool InRange(int ch)
        {
            foreach (var r in _ranges)
            {
                if (r.InRange(ch))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// From PresentationCore.FamilyMap.cs
        /// </summary>
        internal class Range
        {
            internal Range(int first, int last)
            {
                Debug.Assert(first <= last);
                First = first;
                Delta = (uint) (last - First); // used in range testing
            }

            internal bool InRange(int ch)
            {
                // clever code from Word meaning: "ch >= _first && ch <= _last",
                // this is done with one test and branch.
                return (uint) (ch - First) <= Delta;
            }

            internal int First { get; }

            internal int Last => First + (int) Delta;

            internal uint Delta { get; }
        }
    }
}