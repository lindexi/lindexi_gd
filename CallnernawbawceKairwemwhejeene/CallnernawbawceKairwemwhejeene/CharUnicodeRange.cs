using System.Collections.Generic;
using System.Linq;

namespace CallnernawbawceKairwemwhejeene
{
    public static class CharUnicodeRange
    {
        /// <summary>
        /// 获取此字符所在范围
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static UnicodeRange GetUnicodeRange(char ch)
            => GetUnicodeRangeInfo(ch)?.UnicodeRange;

        /// <summary>
        /// 获取此字符所在范围名
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static string GetUnicodeRangeName(char ch)
            => GetUnicodeRangeInfo(ch)?.UnicodeRangeName;

        private static UnicodeRangeInfo GetUnicodeRangeInfo(char ch)
        {
            // 这是英文 数字
            if (UnicodeRangeInfoList[0].UnicodeRange.Contain(ch))
            {
                // UnicodeRangeInfoList[0].UnicodeRange == UnicodeRanges.BasicLatin
                return UnicodeRangeInfoList[0];
            }

            // 119
            var currentIndex = 119;//准备二分，这个值刚好是 中日韩统一表意文字 CJK Unified Ideographs 
            var left = 0;
            var right = UnicodeRangeInfoList.Count;
            while (left <= right)
            {
                var unicodeRangeInfo = UnicodeRangeInfoList[currentIndex];
                var unicodeRange = unicodeRangeInfo.UnicodeRange;
                if (unicodeRange.Contain(ch))
                {
                    return unicodeRangeInfo;
                }
                else if (unicodeRange.FirstCodePoint < ch)
                {
                    // 不落在 unicodeRange 里面，同时比 FirstCodePoint 大的，一定比 unicodeRange.FirstCodePoint + unicodeRange.Length 大
                    left = currentIndex + 1;
                }
                else if (unicodeRange.FirstCodePoint > ch)
                {
                    right = currentIndex - 1;
                }

                currentIndex = (right + left) / 2;
            }

            // 理论上走不到下面分支的，下面分支仅仅只是在上面二分写错的时候，或者数据顺序不对的时候才进入
            foreach (var unicodeRangeInfo in UnicodeRangeInfoList)
            {
                if (unicodeRangeInfo.UnicodeRange.Contain(ch))
                {
                    return unicodeRangeInfo;
                }
            }

            return null;
        }

        private static bool Contain(this UnicodeRange unicodeRange, char ch) =>
            ch >= unicodeRange.FirstCodePoint && ch < (unicodeRange.FirstCodePoint + unicodeRange.Length);

        /// <summary>
        /// 获取字符范围名
        /// </summary>
        /// <param name="unicodeRange"></param>
        /// <returns></returns>
        public static string GetUnicodeRangeName(UnicodeRange unicodeRange) => UnicodeRangeInfoList
            .FirstOrDefault(temp => ReferenceEquals(temp.UnicodeRange, unicodeRange))?.UnicodeRangeName;

        /// <summary>
        /// 按照 FirstCodePoint 排序，看顺序是否是对的，这个方法仅是让你知道这个列表是排序的
        /// </summary>
        /// <returns></returns>
        internal static bool CheckSort()
        {
            var t = UnicodeRangeInfoList.ToList();
            t.Sort((a, b) => a.UnicodeRange.FirstCodePoint.CompareTo(b.UnicodeRange.FirstCodePoint));

            for (var i = 0; i < UnicodeRangeInfoList.Count; i++)
            {
                if (!ReferenceEquals(t[i], UnicodeRangeInfoList[i]))
                {
                    return false;
                }

            }

            return true;
        }

        /// <summary>
        /// 这个列表已经是按照 FirstCodePoint 排过顺序的
        /// </summary>
        private static List<UnicodeRangeInfo> UnicodeRangeInfoList { get; } = new List<UnicodeRangeInfo>()
        {
            //new UnicodeRangeInfo("None",UnicodeRanges.None),
            //new UnicodeRangeInfo("All",UnicodeRanges.All),
            new UnicodeRangeInfo("BasicLatin", UnicodeRanges.BasicLatin),
            new UnicodeRangeInfo("Latin1Supplement", UnicodeRanges.Latin1Supplement),
            new UnicodeRangeInfo("LatinExtendedA", UnicodeRanges.LatinExtendedA),
            new UnicodeRangeInfo("LatinExtendedB", UnicodeRanges.LatinExtendedB),
            new UnicodeRangeInfo("IpaExtensions", UnicodeRanges.IpaExtensions),
            new UnicodeRangeInfo("SpacingModifierLetters", UnicodeRanges.SpacingModifierLetters),
            new UnicodeRangeInfo("CombiningDiacriticalMarks", UnicodeRanges.CombiningDiacriticalMarks),
            new UnicodeRangeInfo("GreekandCoptic", UnicodeRanges.GreekandCoptic),
            new UnicodeRangeInfo("Cyrillic", UnicodeRanges.Cyrillic),
            new UnicodeRangeInfo("CyrillicSupplement", UnicodeRanges.CyrillicSupplement),
            new UnicodeRangeInfo("Armenian", UnicodeRanges.Armenian),
            new UnicodeRangeInfo("Hebrew", UnicodeRanges.Hebrew),
            new UnicodeRangeInfo("Arabic", UnicodeRanges.Arabic),
            new UnicodeRangeInfo("Syriac", UnicodeRanges.Syriac),
            new UnicodeRangeInfo("ArabicSupplement", UnicodeRanges.ArabicSupplement),
            new UnicodeRangeInfo("Thaana", UnicodeRanges.Thaana),
            new UnicodeRangeInfo("NKo", UnicodeRanges.NKo),
            new UnicodeRangeInfo("Samaritan", UnicodeRanges.Samaritan),
            new UnicodeRangeInfo("Mandaic", UnicodeRanges.Mandaic),
            new UnicodeRangeInfo("SyriacSupplement", UnicodeRanges.SyriacSupplement),
            new UnicodeRangeInfo("ArabicExtendedA", UnicodeRanges.ArabicExtendedA),
            new UnicodeRangeInfo("Devanagari", UnicodeRanges.Devanagari),
            new UnicodeRangeInfo("Bengali", UnicodeRanges.Bengali),
            new UnicodeRangeInfo("Gurmukhi", UnicodeRanges.Gurmukhi),
            new UnicodeRangeInfo("Gujarati", UnicodeRanges.Gujarati),
            new UnicodeRangeInfo("Oriya", UnicodeRanges.Oriya),
            new UnicodeRangeInfo("Tamil", UnicodeRanges.Tamil),
            new UnicodeRangeInfo("Telugu", UnicodeRanges.Telugu),
            new UnicodeRangeInfo("Kannada", UnicodeRanges.Kannada),
            new UnicodeRangeInfo("Malayalam", UnicodeRanges.Malayalam),
            new UnicodeRangeInfo("Sinhala", UnicodeRanges.Sinhala),
            new UnicodeRangeInfo("Thai", UnicodeRanges.Thai),
            new UnicodeRangeInfo("Lao", UnicodeRanges.Lao),
            new UnicodeRangeInfo("Tibetan", UnicodeRanges.Tibetan),
            new UnicodeRangeInfo("Myanmar", UnicodeRanges.Myanmar),
            new UnicodeRangeInfo("Georgian", UnicodeRanges.Georgian),
            new UnicodeRangeInfo("HangulJamo", UnicodeRanges.HangulJamo),
            new UnicodeRangeInfo("Ethiopic", UnicodeRanges.Ethiopic),
            new UnicodeRangeInfo("EthiopicSupplement", UnicodeRanges.EthiopicSupplement),
            new UnicodeRangeInfo("Cherokee", UnicodeRanges.Cherokee),
            new UnicodeRangeInfo("UnifiedCanadianAboriginalSyllabics",
                UnicodeRanges.UnifiedCanadianAboriginalSyllabics),
            new UnicodeRangeInfo("Ogham", UnicodeRanges.Ogham),
            new UnicodeRangeInfo("Runic", UnicodeRanges.Runic),
            new UnicodeRangeInfo("Tagalog", UnicodeRanges.Tagalog),
            new UnicodeRangeInfo("Hanunoo", UnicodeRanges.Hanunoo),
            new UnicodeRangeInfo("Buhid", UnicodeRanges.Buhid),
            new UnicodeRangeInfo("Tagbanwa", UnicodeRanges.Tagbanwa),
            new UnicodeRangeInfo("Khmer", UnicodeRanges.Khmer),
            new UnicodeRangeInfo("Mongolian", UnicodeRanges.Mongolian),
            new UnicodeRangeInfo("UnifiedCanadianAboriginalSyllabicsExtended",
                UnicodeRanges.UnifiedCanadianAboriginalSyllabicsExtended),
            new UnicodeRangeInfo("Limbu", UnicodeRanges.Limbu),
            new UnicodeRangeInfo("TaiLe", UnicodeRanges.TaiLe),
            new UnicodeRangeInfo("NewTaiLue", UnicodeRanges.NewTaiLue),
            new UnicodeRangeInfo("KhmerSymbols", UnicodeRanges.KhmerSymbols),
            new UnicodeRangeInfo("Buginese", UnicodeRanges.Buginese),
            new UnicodeRangeInfo("TaiTham", UnicodeRanges.TaiTham),
            new UnicodeRangeInfo("CombiningDiacriticalMarksExtended", UnicodeRanges.CombiningDiacriticalMarksExtended),
            new UnicodeRangeInfo("Balinese", UnicodeRanges.Balinese),
            new UnicodeRangeInfo("Sundanese", UnicodeRanges.Sundanese),
            new UnicodeRangeInfo("Batak", UnicodeRanges.Batak),
            new UnicodeRangeInfo("Lepcha", UnicodeRanges.Lepcha),
            new UnicodeRangeInfo("OlChiki", UnicodeRanges.OlChiki),
            new UnicodeRangeInfo("CyrillicExtendedC", UnicodeRanges.CyrillicExtendedC),
            new UnicodeRangeInfo("GeorgianExtended", UnicodeRanges.GeorgianExtended),
            new UnicodeRangeInfo("SundaneseSupplement", UnicodeRanges.SundaneseSupplement),
            new UnicodeRangeInfo("VedicExtensions", UnicodeRanges.VedicExtensions),
            new UnicodeRangeInfo("PhoneticExtensions", UnicodeRanges.PhoneticExtensions),
            new UnicodeRangeInfo("PhoneticExtensionsSupplement", UnicodeRanges.PhoneticExtensionsSupplement),
            new UnicodeRangeInfo("CombiningDiacriticalMarksSupplement",
                UnicodeRanges.CombiningDiacriticalMarksSupplement),
            new UnicodeRangeInfo("LatinExtendedAdditional", UnicodeRanges.LatinExtendedAdditional),
            new UnicodeRangeInfo("GreekExtended", UnicodeRanges.GreekExtended),
            new UnicodeRangeInfo("GeneralPunctuation", UnicodeRanges.GeneralPunctuation),
            new UnicodeRangeInfo("SuperscriptsandSubscripts", UnicodeRanges.SuperscriptsandSubscripts),
            new UnicodeRangeInfo("CurrencySymbols", UnicodeRanges.CurrencySymbols),
            new UnicodeRangeInfo("CombiningDiacriticalMarksforSymbols",
                UnicodeRanges.CombiningDiacriticalMarksforSymbols),
            new UnicodeRangeInfo("LetterlikeSymbols", UnicodeRanges.LetterlikeSymbols),
            new UnicodeRangeInfo("NumberForms", UnicodeRanges.NumberForms),
            new UnicodeRangeInfo("Arrows", UnicodeRanges.Arrows),
            new UnicodeRangeInfo("MathematicalOperators", UnicodeRanges.MathematicalOperators),
            new UnicodeRangeInfo("MiscellaneousTechnical", UnicodeRanges.MiscellaneousTechnical),
            new UnicodeRangeInfo("ControlPictures", UnicodeRanges.ControlPictures),
            new UnicodeRangeInfo("OpticalCharacterRecognition", UnicodeRanges.OpticalCharacterRecognition),
            new UnicodeRangeInfo("EnclosedAlphanumerics", UnicodeRanges.EnclosedAlphanumerics),
            new UnicodeRangeInfo("BoxDrawing", UnicodeRanges.BoxDrawing),
            new UnicodeRangeInfo("BlockElements", UnicodeRanges.BlockElements),
            new UnicodeRangeInfo("GeometricShapes", UnicodeRanges.GeometricShapes),
            new UnicodeRangeInfo("MiscellaneousSymbols", UnicodeRanges.MiscellaneousSymbols),
            new UnicodeRangeInfo("Dingbats", UnicodeRanges.Dingbats),
            new UnicodeRangeInfo("MiscellaneousMathematicalSymbolsA", UnicodeRanges.MiscellaneousMathematicalSymbolsA),
            new UnicodeRangeInfo("SupplementalArrowsA", UnicodeRanges.SupplementalArrowsA),
            new UnicodeRangeInfo("BraillePatterns", UnicodeRanges.BraillePatterns),
            new UnicodeRangeInfo("SupplementalArrowsB", UnicodeRanges.SupplementalArrowsB),
            new UnicodeRangeInfo("MiscellaneousMathematicalSymbolsB", UnicodeRanges.MiscellaneousMathematicalSymbolsB),
            new UnicodeRangeInfo("SupplementalMathematicalOperators", UnicodeRanges.SupplementalMathematicalOperators),
            new UnicodeRangeInfo("MiscellaneousSymbolsandArrows", UnicodeRanges.MiscellaneousSymbolsandArrows),
            new UnicodeRangeInfo("Glagolitic", UnicodeRanges.Glagolitic),
            new UnicodeRangeInfo("LatinExtendedC", UnicodeRanges.LatinExtendedC),
            new UnicodeRangeInfo("Coptic", UnicodeRanges.Coptic),
            new UnicodeRangeInfo("GeorgianSupplement", UnicodeRanges.GeorgianSupplement),
            new UnicodeRangeInfo("Tifinagh", UnicodeRanges.Tifinagh),
            new UnicodeRangeInfo("EthiopicExtended", UnicodeRanges.EthiopicExtended),
            new UnicodeRangeInfo("CyrillicExtendedA", UnicodeRanges.CyrillicExtendedA),
            new UnicodeRangeInfo("SupplementalPunctuation", UnicodeRanges.SupplementalPunctuation),
            new UnicodeRangeInfo("CjkRadicalsSupplement", UnicodeRanges.CjkRadicalsSupplement),
            new UnicodeRangeInfo("KangxiRadicals", UnicodeRanges.KangxiRadicals),
            new UnicodeRangeInfo("IdeographicDescriptionCharacters", UnicodeRanges.IdeographicDescriptionCharacters),
            new UnicodeRangeInfo("CjkSymbolsandPunctuation", UnicodeRanges.CjkSymbolsandPunctuation),
            new UnicodeRangeInfo("Hiragana", UnicodeRanges.Hiragana),
            new UnicodeRangeInfo("Katakana", UnicodeRanges.Katakana),
            new UnicodeRangeInfo("Bopomofo", UnicodeRanges.Bopomofo),
            new UnicodeRangeInfo("HangulCompatibilityJamo", UnicodeRanges.HangulCompatibilityJamo),
            new UnicodeRangeInfo("Kanbun", UnicodeRanges.Kanbun),
            new UnicodeRangeInfo("BopomofoExtended", UnicodeRanges.BopomofoExtended),
            new UnicodeRangeInfo("CjkStrokes", UnicodeRanges.CjkStrokes),
            new UnicodeRangeInfo("KatakanaPhoneticExtensions", UnicodeRanges.KatakanaPhoneticExtensions),
            new UnicodeRangeInfo("EnclosedCjkLettersandMonths", UnicodeRanges.EnclosedCjkLettersandMonths),
            new UnicodeRangeInfo("CjkCompatibility", UnicodeRanges.CjkCompatibility),
            new UnicodeRangeInfo("CjkUnifiedIdeographsExtensionA", UnicodeRanges.CjkUnifiedIdeographsExtensionA),
            new UnicodeRangeInfo("YijingHexagramSymbols", UnicodeRanges.YijingHexagramSymbols),
            new UnicodeRangeInfo("CjkUnifiedIdeographs", UnicodeRanges.CjkUnifiedIdeographs),
            new UnicodeRangeInfo("YiSyllables", UnicodeRanges.YiSyllables),
            new UnicodeRangeInfo("YiRadicals", UnicodeRanges.YiRadicals),
            new UnicodeRangeInfo("Lisu", UnicodeRanges.Lisu),
            new UnicodeRangeInfo("Vai", UnicodeRanges.Vai),
            new UnicodeRangeInfo("CyrillicExtendedB", UnicodeRanges.CyrillicExtendedB),
            new UnicodeRangeInfo("Bamum", UnicodeRanges.Bamum),
            new UnicodeRangeInfo("ModifierToneLetters", UnicodeRanges.ModifierToneLetters),
            new UnicodeRangeInfo("LatinExtendedD", UnicodeRanges.LatinExtendedD),
            new UnicodeRangeInfo("SylotiNagri", UnicodeRanges.SylotiNagri),
            new UnicodeRangeInfo("CommonIndicNumberForms", UnicodeRanges.CommonIndicNumberForms),
            new UnicodeRangeInfo("Phagspa", UnicodeRanges.Phagspa),
            new UnicodeRangeInfo("Saurashtra", UnicodeRanges.Saurashtra),
            new UnicodeRangeInfo("DevanagariExtended", UnicodeRanges.DevanagariExtended),
            new UnicodeRangeInfo("KayahLi", UnicodeRanges.KayahLi),
            new UnicodeRangeInfo("Rejang", UnicodeRanges.Rejang),
            new UnicodeRangeInfo("HangulJamoExtendedA", UnicodeRanges.HangulJamoExtendedA),
            new UnicodeRangeInfo("Javanese", UnicodeRanges.Javanese),
            new UnicodeRangeInfo("MyanmarExtendedB", UnicodeRanges.MyanmarExtendedB),
            new UnicodeRangeInfo("Cham", UnicodeRanges.Cham),
            new UnicodeRangeInfo("MyanmarExtendedA", UnicodeRanges.MyanmarExtendedA),
            new UnicodeRangeInfo("TaiViet", UnicodeRanges.TaiViet),
            new UnicodeRangeInfo("MeeteiMayekExtensions", UnicodeRanges.MeeteiMayekExtensions),
            new UnicodeRangeInfo("EthiopicExtendedA", UnicodeRanges.EthiopicExtendedA),
            new UnicodeRangeInfo("LatinExtendedE", UnicodeRanges.LatinExtendedE),
            new UnicodeRangeInfo("CherokeeSupplement", UnicodeRanges.CherokeeSupplement),
            new UnicodeRangeInfo("MeeteiMayek", UnicodeRanges.MeeteiMayek),
            new UnicodeRangeInfo("HangulSyllables", UnicodeRanges.HangulSyllables),
            new UnicodeRangeInfo("HangulJamoExtendedB", UnicodeRanges.HangulJamoExtendedB),
            new UnicodeRangeInfo("CjkCompatibilityIdeographs", UnicodeRanges.CjkCompatibilityIdeographs),
            new UnicodeRangeInfo("AlphabeticPresentationForms", UnicodeRanges.AlphabeticPresentationForms),
            new UnicodeRangeInfo("ArabicPresentationFormsA", UnicodeRanges.ArabicPresentationFormsA),
            new UnicodeRangeInfo("VariationSelectors", UnicodeRanges.VariationSelectors),
            new UnicodeRangeInfo("VerticalForms", UnicodeRanges.VerticalForms),
            new UnicodeRangeInfo("CombiningHalfMarks", UnicodeRanges.CombiningHalfMarks),
            new UnicodeRangeInfo("CjkCompatibilityForms", UnicodeRanges.CjkCompatibilityForms),
            new UnicodeRangeInfo("SmallFormVariants", UnicodeRanges.SmallFormVariants),
            new UnicodeRangeInfo("ArabicPresentationFormsB", UnicodeRanges.ArabicPresentationFormsB),
            new UnicodeRangeInfo("HalfwidthandFullwidthForms", UnicodeRanges.HalfwidthandFullwidthForms),
            new UnicodeRangeInfo("Specials", UnicodeRanges.Specials),
        };

        private class UnicodeRangeInfo
        {
            public UnicodeRangeInfo(string unicodeRangeName, UnicodeRange unicodeRange)
            {
                UnicodeRangeName = unicodeRangeName;
                UnicodeRange = unicodeRange;
            }

            public string UnicodeRangeName { get; }

            public UnicodeRange UnicodeRange { get; }
        }
    }
}