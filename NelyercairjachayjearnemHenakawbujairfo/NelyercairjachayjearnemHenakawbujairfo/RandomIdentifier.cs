using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Whitman
{
    public class RandomIdentifier
    {
        public int WordCount
        {
            get => _wordCount;
            set
            {
                if (value <= 2)
                {
                    return;
                }

                _wordCount = value;
            }
        }

        public string Generate(bool pascal)
        {
            var builder = new StringBuilder();
            int wordCount;
            if (_wordCount == 2)
            {
                wordCount = _wordCount;
            }
            else
            {
                wordCount = _random.Next(2, _wordCount);
            }

            for (var i = 0; i < wordCount; i++)
            {
                var syllableCount = 7 - (int) Math.Sqrt(_random.Next(0, 16));
                for (var j = 0; j < syllableCount; j++)
                {
                    var consonant = Consonants[_random.Next(Consonants.Count)];
                    var vowel = Vowels[_random.Next(Vowels.Count)];
                    if ((pascal || i != 0) && j == 0)
                    {
                        consonant = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(consonant);
                    }

                    builder.Append(consonant);
                    builder.Append(vowel);
                }
            }

            return builder.ToString();
        }

        private readonly Random _random = new Random();
        private int _wordCount = 2;

        private static readonly List<string> Consonants = new List<string>
        {
            "q",
            "w",
            "r",
            "y",
            "d",
            "f",
            "g",
            "h",
            "j",
            "k",
            "l",
            "c",
            "b",
            "n",
            "w",
            "f",
            "h",
            "j",
            "k",
            "l",
            "c",
            "n",
            "h",
            "j",
            "k",
            "l",
            "b",
            "n",
            "ch",
            "wh",
        };

        private static readonly List<string> Vowels = new List<string>
        {
            "a",
            "e",
            "i",
            "o",
            "u",
            "a",
            "e",
            "i",
            "o",
            "u",
            "a",
            "e",
            "i",
            "a",
            "e",
            "e",
            "ar",
            "ai",
            "air",
            "ay",
            "al",
            "all",
            "aw",
            "ee",
            "ea",
            "ear",
            "em",
            "er",
            "el",
            "ere",
            "ur"
        };
    }
}