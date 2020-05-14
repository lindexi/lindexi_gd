using System.Runtime.CompilerServices;
using System.Threading;

namespace CallnernawbawceKairwemwhejeene
{
    /// <summary>Provides static properties that return predefined <see cref="T:UnicodeRange" /> instances that correspond to blocks from the Unicode specification.</summary>
    public static class UnicodeRanges
    {
        private static UnicodeRange _none;
        private static UnicodeRange _all;
        private static UnicodeRange _u0000;
        private static UnicodeRange _u0080;
        private static UnicodeRange _u0100;
        private static UnicodeRange _u0180;
        private static UnicodeRange _u0250;
        private static UnicodeRange _u02B0;
        private static UnicodeRange _u0300;
        private static UnicodeRange _u0370;
        private static UnicodeRange _u0400;
        private static UnicodeRange _u0500;
        private static UnicodeRange _u0530;
        private static UnicodeRange _u0590;
        private static UnicodeRange _u0600;
        private static UnicodeRange _u0700;
        private static UnicodeRange _u0750;
        private static UnicodeRange _u0780;
        private static UnicodeRange _u07C0;
        private static UnicodeRange _u0800;
        private static UnicodeRange _u0840;
        private static UnicodeRange _u0860;
        private static UnicodeRange _u08A0;
        private static UnicodeRange _u0900;
        private static UnicodeRange _u0980;
        private static UnicodeRange _u0A00;
        private static UnicodeRange _u0A80;
        private static UnicodeRange _u0B00;
        private static UnicodeRange _u0B80;
        private static UnicodeRange _u0C00;
        private static UnicodeRange _u0C80;
        private static UnicodeRange _u0D00;
        private static UnicodeRange _u0D80;
        private static UnicodeRange _u0E00;
        private static UnicodeRange _u0E80;
        private static UnicodeRange _u0F00;
        private static UnicodeRange _u1000;
        private static UnicodeRange _u10A0;
        private static UnicodeRange _u1100;
        private static UnicodeRange _u1200;
        private static UnicodeRange _u1380;
        private static UnicodeRange _u13A0;
        private static UnicodeRange _u1400;
        private static UnicodeRange _u1680;
        private static UnicodeRange _u16A0;
        private static UnicodeRange _u1700;
        private static UnicodeRange _u1720;
        private static UnicodeRange _u1740;
        private static UnicodeRange _u1760;
        private static UnicodeRange _u1780;
        private static UnicodeRange _u1800;
        private static UnicodeRange _u18B0;
        private static UnicodeRange _u1900;
        private static UnicodeRange _u1950;
        private static UnicodeRange _u1980;
        private static UnicodeRange _u19E0;
        private static UnicodeRange _u1A00;
        private static UnicodeRange _u1A20;
        private static UnicodeRange _u1AB0;
        private static UnicodeRange _u1B00;
        private static UnicodeRange _u1B80;
        private static UnicodeRange _u1BC0;
        private static UnicodeRange _u1C00;
        private static UnicodeRange _u1C50;
        private static UnicodeRange _u1C80;
        private static UnicodeRange _u1C90;
        private static UnicodeRange _u1CC0;
        private static UnicodeRange _u1CD0;
        private static UnicodeRange _u1D00;
        private static UnicodeRange _u1D80;
        private static UnicodeRange _u1DC0;
        private static UnicodeRange _u1E00;
        private static UnicodeRange _u1F00;
        private static UnicodeRange _u2000;
        private static UnicodeRange _u2070;
        private static UnicodeRange _u20A0;
        private static UnicodeRange _u20D0;
        private static UnicodeRange _u2100;
        private static UnicodeRange _u2150;
        private static UnicodeRange _u2190;
        private static UnicodeRange _u2200;
        private static UnicodeRange _u2300;
        private static UnicodeRange _u2400;
        private static UnicodeRange _u2440;
        private static UnicodeRange _u2460;
        private static UnicodeRange _u2500;
        private static UnicodeRange _u2580;
        private static UnicodeRange _u25A0;
        private static UnicodeRange _u2600;
        private static UnicodeRange _u2700;
        private static UnicodeRange _u27C0;
        private static UnicodeRange _u27F0;
        private static UnicodeRange _u2800;
        private static UnicodeRange _u2900;
        private static UnicodeRange _u2980;
        private static UnicodeRange _u2A00;
        private static UnicodeRange _u2B00;
        private static UnicodeRange _u2C00;
        private static UnicodeRange _u2C60;
        private static UnicodeRange _u2C80;
        private static UnicodeRange _u2D00;
        private static UnicodeRange _u2D30;
        private static UnicodeRange _u2D80;
        private static UnicodeRange _u2DE0;
        private static UnicodeRange _u2E00;
        private static UnicodeRange _u2E80;
        private static UnicodeRange _u2F00;
        private static UnicodeRange _u2FF0;
        private static UnicodeRange _u3000;
        private static UnicodeRange _u3040;
        private static UnicodeRange _u30A0;
        private static UnicodeRange _u3100;
        private static UnicodeRange _u3130;
        private static UnicodeRange _u3190;
        private static UnicodeRange _u31A0;
        private static UnicodeRange _u31C0;
        private static UnicodeRange _u31F0;
        private static UnicodeRange _u3200;
        private static UnicodeRange _u3300;
        private static UnicodeRange _u3400;
        private static UnicodeRange _u4DC0;
        private static UnicodeRange _u4E00;
        private static UnicodeRange _uA000;
        private static UnicodeRange _uA490;
        private static UnicodeRange _uA4D0;
        private static UnicodeRange _uA500;
        private static UnicodeRange _uA640;
        private static UnicodeRange _uA6A0;
        private static UnicodeRange _uA700;
        private static UnicodeRange _uA720;
        private static UnicodeRange _uA800;
        private static UnicodeRange _uA830;
        private static UnicodeRange _uA840;
        private static UnicodeRange _uA880;
        private static UnicodeRange _uA8E0;
        private static UnicodeRange _uA900;
        private static UnicodeRange _uA930;
        private static UnicodeRange _uA960;
        private static UnicodeRange _uA980;
        private static UnicodeRange _uA9E0;
        private static UnicodeRange _uAA00;
        private static UnicodeRange _uAA60;
        private static UnicodeRange _uAA80;
        private static UnicodeRange _uAAE0;
        private static UnicodeRange _uAB00;
        private static UnicodeRange _uAB30;
        private static UnicodeRange _uAB70;
        private static UnicodeRange _uABC0;
        private static UnicodeRange _uAC00;
        private static UnicodeRange _uD7B0;
        private static UnicodeRange _uF900;
        private static UnicodeRange _uFB00;
        private static UnicodeRange _uFB50;
        private static UnicodeRange _uFE00;
        private static UnicodeRange _uFE10;
        private static UnicodeRange _uFE20;
        private static UnicodeRange _uFE30;
        private static UnicodeRange _uFE50;
        private static UnicodeRange _uFE70;
        private static UnicodeRange _uFF00;
        private static UnicodeRange _uFFF0;

        /// <summary>Gets an empty Unicode range.</summary>
        /// <returns>A Unicode range with no elements.</returns>
        public static UnicodeRange None
        {
            get { return UnicodeRanges._none ?? UnicodeRanges.CreateEmptyRange(ref UnicodeRanges._none); }
        }

        /// <summary>Gets a range that consists of the entire Basic Multilingual Plane (BMP), from U+0000 to U+FFFF).</summary>
        /// <returns>A range that consists of the entire BMP.</returns>
        public static UnicodeRange All
        {
            get
            {
                return UnicodeRanges._all ??
                       UnicodeRanges.CreateRange(ref UnicodeRanges._all, char.MinValue, char.MaxValue);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UnicodeRange CreateEmptyRange(ref UnicodeRange range)
        {
            Volatile.Write<UnicodeRange>(ref range, new UnicodeRange(0, 0));
            return range;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UnicodeRange CreateRange(
            ref UnicodeRange range,
            char first,
            char last)
        {
            Volatile.Write<UnicodeRange>(ref range, UnicodeRange.Create(first, last));
            return range;
        }

        /// <summary>Gets the Basic Latin Unicode block (U+0021-U+007F).</summary>
        /// <returns>The Basic Latin Unicode block (U+0021-U+007F).</returns>
        public static UnicodeRange BasicLatin
        {
            get
            {
                return UnicodeRanges._u0000 ??
                       UnicodeRanges.CreateRange(ref UnicodeRanges._u0000, char.MinValue, '\x007F');
            }
        }

        /// <summary>Gets the Latin-1 Supplement Unicode block (U+00A1-U+00FF).</summary>
        /// <returns>The Latin-1 Supplement Unicode block (U+00A1-U+00FF).</returns>
        public static UnicodeRange Latin1Supplement
        {
            get { return UnicodeRanges._u0080 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0080, '\x0080', 'ÿ'); }
        }

        /// <summary>Gets the Latin Extended-A Unicode block (U+0100-U+017F).</summary>
        /// <returns>The Latin Extended-A Unicode block (U+0100-U+017F).</returns>
        public static UnicodeRange LatinExtendedA
        {
            get { return UnicodeRanges._u0100 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0100, 'Ā', 'ſ'); }
        }

        /// <summary>Gets the Latin Extended-B Unicode block (U+0180-U+024F).</summary>
        /// <returns>The Latin Extended-B Unicode block (U+0180-U+024F).</returns>
        public static UnicodeRange LatinExtendedB
        {
            get { return UnicodeRanges._u0180 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0180, 'ƀ', 'ɏ'); }
        }

        /// <summary>Gets the IPA Extensions Unicode block (U+0250-U+02AF).</summary>
        /// <returns>The IPA Extensions Unicode block (U+0250-U+02AF).</returns>
        public static UnicodeRange IpaExtensions
        {
            get { return UnicodeRanges._u0250 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0250, 'ɐ', 'ʯ'); }
        }

        /// <summary>Gets the Spacing Modifier Letters Unicode block (U+02B0-U+02FF).</summary>
        /// <returns>The Spacing Modifier Letters Unicode block (U+02B0-U+02FF).</returns>
        public static UnicodeRange SpacingModifierLetters
        {
            get { return UnicodeRanges._u02B0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u02B0, 'ʰ', '˿'); }
        }

        /// <summary>Gets the Combining Diacritical Marks Unicode block (U+0300-U+036F).</summary>
        /// <returns>The Combining Diacritical Marks Unicode block (U+0300-U+036F).</returns>
        public static UnicodeRange CombiningDiacriticalMarks
        {
            get { return UnicodeRanges._u0300 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0300, '̀', 'ͯ'); }
        }

        /// <summary>Gets the Greek and Coptic Unicode block (U+0370-U+03FF).</summary>
        /// <returns>The Greek and Coptic Unicode block (U+0370-U+03FF).</returns>
        public static UnicodeRange GreekandCoptic
        {
            get { return UnicodeRanges._u0370 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0370, 'Ͱ', 'Ͽ'); }
        }

        /// <summary>Gets the Cyrillic Unicode block (U+0400-U+04FF).</summary>
        /// <returns>The Cyrillic Unicode block (U+0400-U+04FF).</returns>
        public static UnicodeRange Cyrillic
        {
            get { return UnicodeRanges._u0400 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0400, 'Ѐ', 'ӿ'); }
        }

        /// <summary>Gets the Cyrillic Supplement Unicode block (U+0500-U+052F).</summary>
        /// <returns>The Cyrillic Supplement Unicode block (U+0500-U+052F).</returns>
        public static UnicodeRange CyrillicSupplement
        {
            get { return UnicodeRanges._u0500 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0500, 'Ԁ', 'ԯ'); }
        }

        /// <summary>Gets the Armenian Unicode block (U+0530-U+058F).</summary>
        /// <returns>The Armenian Unicode block (U+0530-U+058F).</returns>
        public static UnicodeRange Armenian
        {
            get { return UnicodeRanges._u0530 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0530, '\x0530', '֏'); }
        }

        /// <summary>Gets the Hebrew Unicode block (U+0590-U+05FF).</summary>
        /// <returns>The Hebrew Unicode block (U+0590-U+05FF).</returns>
        public static UnicodeRange Hebrew
        {
            get
            {
                return UnicodeRanges._u0590 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0590, '\x0590', '\x05FF');
            }
        }

        /// <summary>Gets the Arabic Unicode block (U+0600-U+06FF).</summary>
        /// <returns>The Arabic Unicode block (U+0600-U+06FF).</returns>
        public static UnicodeRange Arabic
        {
            get { return UnicodeRanges._u0600 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0600, '\x0600', 'ۿ'); }
        }

        /// <summary>Gets the Syriac Unicode block (U+0700-U+074F).</summary>
        /// <returns>The Syriac Unicode block (U+0700-U+074F).</returns>
        public static UnicodeRange Syriac
        {
            get { return UnicodeRanges._u0700 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0700, '܀', 'ݏ'); }
        }

        /// <summary>Gets the Arabic Supplement Unicode block (U+0750-U+077F).</summary>
        /// <returns>The Arabic Supplement Unicode block (U+0750-U+077F).</returns>
        public static UnicodeRange ArabicSupplement
        {
            get { return UnicodeRanges._u0750 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0750, 'ݐ', 'ݿ'); }
        }

        /// <summary>Gets the Thaana Unicode block (U+0780-U+07BF).</summary>
        /// <returns>The Thaana Unicode block (U+0780-U+07BF).</returns>
        public static UnicodeRange Thaana
        {
            get { return UnicodeRanges._u0780 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0780, 'ހ', '\x07BF'); }
        }

        /// <summary>Gets the NKo Unicode block (U+07C0-U+07FF).</summary>
        /// <returns>The NKo Unicode block (U+07C0-U+07FF).</returns>
        public static UnicodeRange NKo
        {
            get { return UnicodeRanges._u07C0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u07C0, '߀', '\x07FF'); }
        }

        /// <summary>Gets the Samaritan Unicode block (U+0800-U+083F).</summary>
        /// <returns>The Samaritan Unicode block (U+0800-U+083F).</returns>
        public static UnicodeRange Samaritan
        {
            get { return UnicodeRanges._u0800 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0800, 'ࠀ', '\x083F'); }
        }

        /// <summary>Gets the Mandaic Unicode block (U+0840-U+085F).</summary>
        /// <returns>The Mandaic Unicode block (U+0840-U+085F).</returns>
        public static UnicodeRange Mandaic
        {
            get { return UnicodeRanges._u0840 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0840, 'ࡀ', '\x085F'); }
        }

        /// <summary>A <see cref="T:UnicodeRange" /> corresponding to the 'Syriac Supplement' Unicode block (U+0860..U+086F).</summary>
        public static UnicodeRange SyriacSupplement
        {
            get
            {
                return UnicodeRanges._u0860 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0860, '\x0860', '\x086F');
            }
        }

        /// <summary>Gets the Arabic Extended-A Unicode block (U+08A0-U+08FF).</summary>
        /// <returns>The Arabic Extended-A Unicode block (U+08A0-U+08FF).</returns>
        public static UnicodeRange ArabicExtendedA
        {
            get { return UnicodeRanges._u08A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u08A0, 'ࢠ', 'ࣿ'); }
        }

        /// <summary>Gets the Devangari Unicode block (U+0900-U+097F).</summary>
        /// <returns>The Devangari Unicode block (U+0900-U+097F).</returns>
        public static UnicodeRange Devanagari
        {
            get { return UnicodeRanges._u0900 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0900, 'ऀ', 'ॿ'); }
        }

        /// <summary>Gets the Bengali Unicode block (U+0980-U+09FF).</summary>
        /// <returns>The Bengali Unicode block (U+0980-U+09FF).</returns>
        public static UnicodeRange Bengali
        {
            get { return UnicodeRanges._u0980 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0980, 'ঀ', '\x09FF'); }
        }

        /// <summary>Gets the Gurmukhi Unicode block (U+0A01-U+0A7F).</summary>
        /// <returns>The Gurmukhi Unicode block (U+0A01-U+0A7F).</returns>
        public static UnicodeRange Gurmukhi
        {
            get
            {
                return UnicodeRanges._u0A00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0A00, '\x0A00', '\x0A7F');
            }
        }

        /// <summary>Gets the Gujarti Unicode block (U+0A81-U+0AFF).</summary>
        /// <returns>The Gujarti Unicode block (U+0A81-U+0AFF).</returns>
        public static UnicodeRange Gujarati
        {
            get
            {
                return UnicodeRanges._u0A80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0A80, '\x0A80', '\x0AFF');
            }
        }

        /// <summary>Gets the Oriya Unicode block (U+0B00-U+0B7F).</summary>
        /// <returns>The Oriya Unicode block (U+0B00-U+0B7F).</returns>
        public static UnicodeRange Oriya
        {
            get
            {
                return UnicodeRanges._u0B00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0B00, '\x0B00', '\x0B7F');
            }
        }

        /// <summary>Gets the Tamil Unicode block (U+0B80-U+0BFF).</summary>
        /// <returns>The Tamil Unicode block (U+0B82-U+0BFA).</returns>
        public static UnicodeRange Tamil
        {
            get
            {
                return UnicodeRanges._u0B80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0B80, '\x0B80', '\x0BFF');
            }
        }

        /// <summary>Gets the Telugu Unicode block (U+0C00-U+0C7F).</summary>
        /// <returns>The Telugu Unicode block (U+0C00-U+0C7F).</returns>
        public static UnicodeRange Telugu
        {
            get { return UnicodeRanges._u0C00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0C00, 'ఀ', '౿'); }
        }

        /// <summary>Gets the Kannada Unicode block (U+0C81-U+0CFF).</summary>
        /// <returns>The Kannada Unicode block (U+0C81-U+0CFF).</returns>
        public static UnicodeRange Kannada
        {
            get
            {
                return UnicodeRanges._u0C80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0C80, '\x0C80', '\x0CFF');
            }
        }

        /// <summary>Gets the Malayalam Unicode block (U+0D00-U+0D7F).</summary>
        /// <returns>The Malayalam Unicode block (U+0D00-U+0D7F).</returns>
        public static UnicodeRange Malayalam
        {
            get { return UnicodeRanges._u0D00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0D00, '\x0D00', 'ൿ'); }
        }

        /// <summary>Gets the Sinhala Unicode block (U+0D80-U+0DFF).</summary>
        /// <returns>The Sinhala Unicode block (U+0D80-U+0DFF).</returns>
        public static UnicodeRange Sinhala
        {
            get
            {
                return UnicodeRanges._u0D80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0D80, '\x0D80', '\x0DFF');
            }
        }

        /// <summary>Gets the Thai Unicode block (U+0E00-U+0E7F).</summary>
        /// <returns>The Thai Unicode block (U+0E00-U+0E7F).</returns>
        public static UnicodeRange Thai
        {
            get
            {
                return UnicodeRanges._u0E00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0E00, '\x0E00', '\x0E7F');
            }
        }

        /// <summary>Gets the Lao Unicode block (U+0E80-U+0EDF).</summary>
        /// <returns>The Lao Unicode block (U+0E80-U+0EDF).</returns>
        public static UnicodeRange Lao
        {
            get
            {
                return UnicodeRanges._u0E80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0E80, '\x0E80', '\x0EFF');
            }
        }

        /// <summary>Gets the Tibetan Unicode block (U+0F00-U+0FFF).</summary>
        /// <returns>The Tibetan Unicode block (U+0F00-U+0FFF).</returns>
        public static UnicodeRange Tibetan
        {
            get { return UnicodeRanges._u0F00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u0F00, 'ༀ', '\x0FFF'); }
        }

        /// <summary>Gets the Myanmar Unicode block (U+1000-U+109F).</summary>
        /// <returns>The Myanmar Unicode block (U+1000-U+109F).</returns>
        public static UnicodeRange Myanmar
        {
            get { return UnicodeRanges._u1000 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1000, 'က', '႟'); }
        }

        /// <summary>Gets the Georgian Unicode block (U+10A0-U+10FF).</summary>
        /// <returns>The Georgian Unicode block (U+10A0-U+10FF).</returns>
        public static UnicodeRange Georgian
        {
            get { return UnicodeRanges._u10A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u10A0, 'Ⴀ', 'ჿ'); }
        }

        /// <summary>Gets the Hangul Jamo Unicode block (U+1100-U+11FF).</summary>
        /// <returns>The Hangul Jamo Unicode block (U+1100-U+11FF).</returns>
        public static UnicodeRange HangulJamo
        {
            get { return UnicodeRanges._u1100 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1100, 'ᄀ', 'ᇿ'); }
        }

        /// <summary>Gets the Ethiopic Unicode block (U+1200-U+137C).</summary>
        /// <returns>The Ethiopic Unicode block (U+1200-U+137C).</returns>
        public static UnicodeRange Ethiopic
        {
            get { return UnicodeRanges._u1200 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1200, 'ሀ', '\x137F'); }
        }

        /// <summary>Gets the Ethiopic Supplement Unicode block (U+1380-U+1399).</summary>
        /// <returns>The Ethiopic Supplement Unicode block (U+1380-U+1399).</returns>
        public static UnicodeRange EthiopicSupplement
        {
            get { return UnicodeRanges._u1380 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1380, 'ᎀ', '\x139F'); }
        }

        /// <summary>Gets the Cherokee Unicode block (U+13A0-U+13FF).</summary>
        /// <returns>The Cherokee Unicode block (U+13A0-U+13FF).</returns>
        public static UnicodeRange Cherokee
        {
            get { return UnicodeRanges._u13A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u13A0, 'Ꭰ', '\x13FF'); }
        }

        /// <summary>Gets the Unified Canadian Aboriginal Syllabics Unicode block (U+1400-U+167F).</summary>
        /// <returns>The Unified Canadian Aboriginal Syllabics Unicode block (U+1400-U+167F).</returns>
        public static UnicodeRange UnifiedCanadianAboriginalSyllabics
        {
            get { return UnicodeRanges._u1400 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1400, '᐀', 'ᙿ'); }
        }

        /// <summary>Gets the Ogham Unicode block (U+1680-U+169F).</summary>
        /// <returns>The Ogham Unicode block (U+1680-U+169F).</returns>
        public static UnicodeRange Ogham
        {
            get { return UnicodeRanges._u1680 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1680, ' ', '\x169F'); }
        }

        /// <summary>Gets the Runic Unicode block (U+16A0-U+16FF).</summary>
        /// <returns>The Runic Unicode block (U+16A0-U+16FF).</returns>
        public static UnicodeRange Runic
        {
            get { return UnicodeRanges._u16A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u16A0, 'ᚠ', '\x16FF'); }
        }

        /// <summary>Gets the Tagalog Unicode block (U+1700-U+171F).</summary>
        /// <returns>The Tagalog Unicode block (U+1700-U+171F).</returns>
        public static UnicodeRange Tagalog
        {
            get { return UnicodeRanges._u1700 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1700, 'ᜀ', '\x171F'); }
        }

        /// <summary>Gets the Hanunoo Unicode block (U+1720-U+173F).</summary>
        /// <returns>The Hanunoo Unicode block (U+1720-U+173F).</returns>
        public static UnicodeRange Hanunoo
        {
            get { return UnicodeRanges._u1720 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1720, 'ᜠ', '\x173F'); }
        }

        /// <summary>Gets the Buhid Unicode block (U+1740-U+175F).</summary>
        /// <returns>The Buhid Unicode block (U+1740-U+175F).</returns>
        public static UnicodeRange Buhid
        {
            get { return UnicodeRanges._u1740 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1740, 'ᝀ', '\x175F'); }
        }

        /// <summary>Gets the Tagbanwa Unicode block (U+1760-U+177F).</summary>
        /// <returns>The Tagbanwa Unicode block (U+1760-U+177F).</returns>
        public static UnicodeRange Tagbanwa
        {
            get { return UnicodeRanges._u1760 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1760, 'ᝠ', '\x177F'); }
        }

        /// <summary>Gets the Khmer Unicode block (U+1780-U+17FF).</summary>
        /// <returns>The Khmer Unicode block (U+1780-U+17FF).</returns>
        public static UnicodeRange Khmer
        {
            get { return UnicodeRanges._u1780 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1780, 'ក', '\x17FF'); }
        }

        /// <summary>Gets the Mongolian Unicode block (U+1800-U+18AF).</summary>
        /// <returns>The Mongolian Unicode block (U+1800-U+18AF).</returns>
        public static UnicodeRange Mongolian
        {
            get { return UnicodeRanges._u1800 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1800, '᠀', '\x18AF'); }
        }

        /// <summary>Gets the Unified Canadian Aboriginal Syllabics Extended Unicode block (U+18B0-U+18FF).</summary>
        /// <returns>The Unified Canadian Aboriginal Syllabics Extended Unicode block (U+18B0-U+18FF).</returns>
        public static UnicodeRange UnifiedCanadianAboriginalSyllabicsExtended
        {
            get { return UnicodeRanges._u18B0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u18B0, 'ᢰ', '\x18FF'); }
        }

        /// <summary>Gets the Limbu Unicode block (U+1900-U+194F).</summary>
        /// <returns>The Limbu Unicode block (U+1900-U+194F).</returns>
        public static UnicodeRange Limbu
        {
            get { return UnicodeRanges._u1900 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1900, 'ᤀ', '᥏'); }
        }

        /// <summary>Gets the Tai Le Unicode block (U+1950-U+197F).</summary>
        /// <returns>The Tai Le Unicode block (U+1950-U+197F).</returns>
        public static UnicodeRange TaiLe
        {
            get { return UnicodeRanges._u1950 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1950, 'ᥐ', '\x197F'); }
        }

        /// <summary>Gets the New Tai Lue Unicode block (U+1980-U+19DF).</summary>
        /// <returns>The New Tai Lue Unicode block (U+1980-U+19DF).</returns>
        public static UnicodeRange NewTaiLue
        {
            get { return UnicodeRanges._u1980 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1980, 'ᦀ', '᧟'); }
        }

        /// <summary>Gets the Khmer Symbols Unicode block (U+19E0-U+19FF).</summary>
        /// <returns>The Khmer Symbols Unicode block (U+19E0-U+19FF).</returns>
        public static UnicodeRange KhmerSymbols
        {
            get { return UnicodeRanges._u19E0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u19E0, '᧠', '᧿'); }
        }

        /// <summary>Gets the Buginese Unicode block (U+1A00-U+1A1F).</summary>
        /// <returns>The Buginese Unicode block (U+1A00-U+1A1F).</returns>
        public static UnicodeRange Buginese
        {
            get { return UnicodeRanges._u1A00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1A00, 'ᨀ', '᨟'); }
        }

        /// <summary>Gets the Tai Tham Unicode block (U+1A20-U+1AAF).</summary>
        /// <returns>The Tai Tham Unicode block (U+1A20-U+1AAF).</returns>
        public static UnicodeRange TaiTham
        {
            get { return UnicodeRanges._u1A20 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1A20, 'ᨠ', '\x1AAF'); }
        }

        /// <summary>Gets the Combining Diacritical Marks Extended Unicode block (U+1AB0-U+1AFF).</summary>
        /// <returns>The Combining Diacritical Marks Extended Unicode block (U+1AB0-U+1AFF).</returns>
        public static UnicodeRange CombiningDiacriticalMarksExtended
        {
            get { return UnicodeRanges._u1AB0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1AB0, '᪰', '\x1AFF'); }
        }

        /// <summary>Gets the Balinese Unicode block (U+1B00-U+1B7F).</summary>
        /// <returns>The Balinese Unicode block (U+1B00-U+1B7F).</returns>
        public static UnicodeRange Balinese
        {
            get { return UnicodeRanges._u1B00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1B00, 'ᬀ', '\x1B7F'); }
        }

        /// <summary>Gets the Sundanese Unicode block (U+1B80-U+1BBF).</summary>
        /// <returns>The Sundanese Unicode block (U+1B80-U+1BBF).</returns>
        public static UnicodeRange Sundanese
        {
            get { return UnicodeRanges._u1B80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1B80, 'ᮀ', 'ᮿ'); }
        }

        /// <summary>Gets the Batak Unicode block (U+1BC0-U+1BFF).</summary>
        /// <returns>The Batak Unicode block (U+1BC0-U+1BFF).</returns>
        public static UnicodeRange Batak
        {
            get { return UnicodeRanges._u1BC0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1BC0, 'ᯀ', '᯿'); }
        }

        /// <summary>Gets the Lepcha Unicode block (U+1C00-U+1C4F).</summary>
        /// <returns>The Lepcha Unicode block (U+1C00-U+1C4F).</returns>
        public static UnicodeRange Lepcha
        {
            get { return UnicodeRanges._u1C00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1C00, 'ᰀ', 'ᱏ'); }
        }

        /// <summary>Gets the Ol Chiki Unicode block (U+1C50-U+1C7F).</summary>
        /// <returns>The Ol Chiki Unicode block (U+1C50-U+1C7F).</returns>
        public static UnicodeRange OlChiki
        {
            get { return UnicodeRanges._u1C50 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1C50, '᱐', '᱿'); }
        }

        /// <summary>A <see cref="T:UnicodeRange" /> corresponding to the 'Cyrillic Extended-C' Unicode block (U+1C80..U+1C8F).</summary>
        public static UnicodeRange CyrillicExtendedC
        {
            get
            {
                return UnicodeRanges._u1C80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1C80, '\x1C80', '\x1C8F');
            }
        }

        /// <summary>A <see cref="T:UnicodeRange" /> corresponding to the 'Georgian Extended' Unicode block (U+1C90..U+1CBF).</summary>
        public static UnicodeRange GeorgianExtended
        {
            get
            {
                return UnicodeRanges._u1C90 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1C90, '\x1C90', '\x1CBF');
            }
        }

        /// <summary>Gets the Sundanese Supplement Unicode block (U+1CC0-U+1CCF).</summary>
        /// <returns>The Sundanese Supplement Unicode block (U+1CC0-U+1CCF).</returns>
        public static UnicodeRange SundaneseSupplement
        {
            get { return UnicodeRanges._u1CC0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1CC0, '᳀', '\x1CCF'); }
        }

        /// <summary>Gets the Vedic Extensions Unicode block (U+1CD0-U+1CFF).</summary>
        /// <returns>The Vedic Extensions Unicode block (U+1CD0-U+1CFF).</returns>
        public static UnicodeRange VedicExtensions
        {
            get { return UnicodeRanges._u1CD0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1CD0, '᳐', '\x1CFF'); }
        }

        /// <summary>Gets the Phonetic Extensions Unicode block (U+1D00-U+1D7F).</summary>
        /// <returns>The Phonetic Extensions Unicode block (U+1D00-U+1D7F).</returns>
        public static UnicodeRange PhoneticExtensions
        {
            get { return UnicodeRanges._u1D00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1D00, 'ᴀ', 'ᵿ'); }
        }

        /// <summary>Gets the Phonetic Extensions Supplement Unicode block (U+1D80-U+1DBF).</summary>
        /// <returns>The Phonetic Extensions Supplement Unicode block (U+1D80-U+1DBF).</returns>
        public static UnicodeRange PhoneticExtensionsSupplement
        {
            get { return UnicodeRanges._u1D80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1D80, 'ᶀ', 'ᶿ'); }
        }

        /// <summary>Gets the Combining Diacritical Marks Supplement Unicode block (U+1DC0-U+1DFF).</summary>
        /// <returns>The Combining Diacritical Marks Supplement Unicode block (U+1DC0-U+1DFF).</returns>
        public static UnicodeRange CombiningDiacriticalMarksSupplement
        {
            get { return UnicodeRanges._u1DC0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1DC0, '᷀', '᷿'); }
        }

        /// <summary>Gets the Latin Extended Additional Unicode block (U+1E00-U+1EFF).</summary>
        /// <returns>The Latin Extended Additional Unicode block (U+1E00-U+1EFF).</returns>
        public static UnicodeRange LatinExtendedAdditional
        {
            get { return UnicodeRanges._u1E00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1E00, 'Ḁ', 'ỿ'); }
        }

        /// <summary>Gets the Greek Extended Unicode block (U+1F00-U+1FFF).</summary>
        /// <returns>The Greek Extended Unicode block (U+1F00-U+1FFF).</returns>
        public static UnicodeRange GreekExtended
        {
            get { return UnicodeRanges._u1F00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u1F00, 'ἀ', '\x1FFF'); }
        }

        /// <summary>Gets the General Punctuation Unicode block (U+2000-U+206F).</summary>
        /// <returns>The General Punctuation Unicode block (U+2000-U+206F).</returns>
        public static UnicodeRange GeneralPunctuation
        {
            get { return UnicodeRanges._u2000 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2000, ' ', '\x206F'); }
        }

        /// <summary>Gets the Superscripts and Subscripts Unicode block (U+2070-U+209F).</summary>
        /// <returns>The Superscripts and Subscripts Unicode block (U+2070-U+209F).</returns>
        public static UnicodeRange SuperscriptsandSubscripts
        {
            get
            {
                return UnicodeRanges._u2070 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2070, '\x2070', '\x209F');
            }
        }

        /// <summary>Gets the Currency Symbols Unicode block (U+20A0-U+20CF).</summary>
        /// <returns>The Currency Symbols Unicode block (U+20A0-U+20CF).</returns>
        public static UnicodeRange CurrencySymbols
        {
            get { return UnicodeRanges._u20A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u20A0, '₠', '\x20CF'); }
        }

        /// <summary>Gets the Combining Diacritical Marks for Symbols Unicode block (U+20D0-U+20FF).</summary>
        /// <returns>The Combining Diacritical Marks for Symbols Unicode block (U+20D0-U+20FF).</returns>
        public static UnicodeRange CombiningDiacriticalMarksforSymbols
        {
            get { return UnicodeRanges._u20D0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u20D0, '⃐', '\x20FF'); }
        }

        /// <summary>Gets the Letterlike Symbols Unicode block (U+2100-U+214F).</summary>
        /// <returns>The Letterlike Symbols Unicode block (U+2100-U+214F).</returns>
        public static UnicodeRange LetterlikeSymbols
        {
            get { return UnicodeRanges._u2100 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2100, '℀', '⅏'); }
        }

        /// <summary>Gets the Number Forms Unicode block (U+2150-U+218F).</summary>
        /// <returns>The Number Forms Unicode block (U+2150-U+218F).</returns>
        public static UnicodeRange NumberForms
        {
            get
            {
                return UnicodeRanges._u2150 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2150, '\x2150', '\x218F');
            }
        }

        /// <summary>Gets the Arrows Unicode block (U+2190-U+21FF).</summary>
        /// <returns>The Arrows Unicode block (U+2190-U+21FF).</returns>
        public static UnicodeRange Arrows
        {
            get { return UnicodeRanges._u2190 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2190, '←', '⇿'); }
        }

        /// <summary>Gets the Mathematical Operators Unicode block (U+2200-U+22FF).</summary>
        /// <returns>The Mathematical Operators Unicode block (U+2200-U+22FF).</returns>
        public static UnicodeRange MathematicalOperators
        {
            get { return UnicodeRanges._u2200 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2200, '∀', '⋿'); }
        }

        /// <summary>Gets the Miscellaneous Technical Unicode block (U+2300-U+23FF).</summary>
        /// <returns>The Miscellaneous Technical Unicode block (U+2300-U+23FF).</returns>
        public static UnicodeRange MiscellaneousTechnical
        {
            get { return UnicodeRanges._u2300 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2300, '⌀', '\x23FF'); }
        }

        /// <summary>Gets the Control Pictures Unicode block (U+2400-U+243F).</summary>
        /// <returns>The Control Pictures Unicode block (U+2400-U+243F).</returns>
        public static UnicodeRange ControlPictures
        {
            get { return UnicodeRanges._u2400 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2400, '␀', '\x243F'); }
        }

        /// <summary>Gets the Optical Character Recognition Unicode block (U+2440-U+245F).</summary>
        /// <returns>The Optical Character Recognition Unicode block (U+2440-U+245F).</returns>
        public static UnicodeRange OpticalCharacterRecognition
        {
            get { return UnicodeRanges._u2440 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2440, '⑀', '\x245F'); }
        }

        /// <summary>Gets the Enclosed Alphanumerics Unicode block (U+2460-U+24FF).</summary>
        /// <returns>The Enclosed Alphanumerics Unicode block (U+2460-U+24FF).</returns>
        public static UnicodeRange EnclosedAlphanumerics
        {
            get
            {
                return UnicodeRanges._u2460 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2460, '\x2460', '\x24FF');
            }
        }

        /// <summary>Gets the Box Drawing Unicode block (U+2500-U+257F).</summary>
        /// <returns>The Box Drawing Unicode block (U+2500-U+257F).</returns>
        public static UnicodeRange BoxDrawing
        {
            get { return UnicodeRanges._u2500 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2500, '─', '╿'); }
        }

        /// <summary>Gets the Block Elements Unicode block (U+2580-U+259F).</summary>
        /// <returns>The Block Elements Unicode block (U+2580-U+259F).</returns>
        public static UnicodeRange BlockElements
        {
            get { return UnicodeRanges._u2580 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2580, '▀', '▟'); }
        }

        /// <summary>Gets the Geometric Shapes Unicode block (U+25A0-U+25FF).</summary>
        /// <returns>The Geometric Shapes Unicode block (U+25A0-U+25FF).</returns>
        public static UnicodeRange GeometricShapes
        {
            get { return UnicodeRanges._u25A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u25A0, '■', '◿'); }
        }

        /// <summary>Gets the Miscellaneous Symbols Unicode block (U+2600-U+26FF).</summary>
        /// <returns>The Miscellaneous Symbols Unicode block (U+2600-U+26FF).</returns>
        public static UnicodeRange MiscellaneousSymbols
        {
            get { return UnicodeRanges._u2600 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2600, '☀', '⛿'); }
        }

        /// <summary>Gets the Dingbats Unicode block (U+2700-U+27BF).</summary>
        /// <returns>The Dingbats Unicode block (U+2700-U+27BF).</returns>
        public static UnicodeRange Dingbats
        {
            get { return UnicodeRanges._u2700 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2700, '✀', '➿'); }
        }

        /// <summary>Gets the Miscellaneous Mathematical Symbols-A Unicode block (U+27C0-U+27EF).</summary>
        /// <returns>The Miscellaneous Mathematical Symbols-A Unicode block (U+27C0-U+27EF).</returns>
        public static UnicodeRange MiscellaneousMathematicalSymbolsA
        {
            get { return UnicodeRanges._u27C0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u27C0, '⟀', '⟯'); }
        }

        /// <summary>Gets the Supplemental Arrows-A Unicode block (U+27F0-U+27FF).</summary>
        /// <returns>The Supplemental Arrows-A Unicode block (U+27F0-U+27FF).</returns>
        public static UnicodeRange SupplementalArrowsA
        {
            get { return UnicodeRanges._u27F0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u27F0, '⟰', '⟿'); }
        }

        /// <summary>Gets the Braille Patterns Unicode block (U+2800-U+28FF).</summary>
        /// <returns>The Braille Patterns Unicode block (U+2800-U+28FF).</returns>
        public static UnicodeRange BraillePatterns
        {
            get { return UnicodeRanges._u2800 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2800, '⠀', '⣿'); }
        }

        /// <summary>Gets the Supplemental Arrows-B Unicode block (U+2900-U+297F).</summary>
        /// <returns>The Supplemental Arrows-B Unicode block (U+2900-U+297F).</returns>
        public static UnicodeRange SupplementalArrowsB
        {
            get { return UnicodeRanges._u2900 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2900, '⤀', '⥿'); }
        }

        /// <summary>Gets the Miscellaneous Mathematical Symbols-B Unicode block (U+2980-U+29FF).</summary>
        /// <returns>The Miscellaneous Mathematical Symbols-B Unicode block (U+2980-U+29FF).</returns>
        public static UnicodeRange MiscellaneousMathematicalSymbolsB
        {
            get { return UnicodeRanges._u2980 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2980, '⦀', '⧿'); }
        }

        /// <summary>Gets the Supplemental Mathematical Operators Unicode block (U+2A00-U+2AFF).</summary>
        /// <returns>The Supplemental Mathematical Operators Unicode block (U+2A00-U+2AFF).</returns>
        public static UnicodeRange SupplementalMathematicalOperators
        {
            get { return UnicodeRanges._u2A00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2A00, '⨀', '⫿'); }
        }

        /// <summary>Gets the Miscellaneous Symbols and Arrows Unicode block (U+2B00-U+2BFF).</summary>
        /// <returns>The Miscellaneous Symbols and Arrows Unicode block (U+2B00-U+2BFF).</returns>
        public static UnicodeRange MiscellaneousSymbolsandArrows
        {
            get { return UnicodeRanges._u2B00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2B00, '⬀', '\x2BFF'); }
        }

        /// <summary>Gets the Glagolitic Unicode block (U+2C00-U+2C5F).</summary>
        /// <returns>The Glagolitic Unicode block (U+2C00-U+2C5F).</returns>
        public static UnicodeRange Glagolitic
        {
            get { return UnicodeRanges._u2C00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2C00, 'Ⰰ', '\x2C5F'); }
        }

        /// <summary>Gets the Latin Extended-C Unicode block (U+2C60-U+2C7F).</summary>
        /// <returns>The Latin Extended-C Unicode block (U+2C60-U+2C7F).</returns>
        public static UnicodeRange LatinExtendedC
        {
            get { return UnicodeRanges._u2C60 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2C60, 'Ⱡ', 'Ɀ'); }
        }

        /// <summary>Gets the Coptic Unicode block (U+2C80-U+2CFF).</summary>
        /// <returns>The Coptic Unicode block (U+2C80-U+2CFF).</returns>
        public static UnicodeRange Coptic
        {
            get { return UnicodeRanges._u2C80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2C80, 'Ⲁ', '⳿'); }
        }

        /// <summary>Gets the Georgian Supplement Unicode block (U+2D00-U+2D2F).</summary>
        /// <returns>The Georgian Supplement Unicode block (U+2D00-U+2D2F).</returns>
        public static UnicodeRange GeorgianSupplement
        {
            get { return UnicodeRanges._u2D00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2D00, 'ⴀ', '\x2D2F'); }
        }

        /// <summary>Gets the Tifinagh Unicode block (U+2D30-U+2D7F).</summary>
        /// <returns>The Tifinagh Unicode block (U+2D30-U+2D7F).</returns>
        public static UnicodeRange Tifinagh
        {
            get { return UnicodeRanges._u2D30 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2D30, 'ⴰ', '⵿'); }
        }

        /// <summary>Gets the Ethipic Extended Unicode block (U+2D80-U+2DDF).</summary>
        /// <returns>The Ethipic Extended Unicode block (U+2D80-U+2DDF).</returns>
        public static UnicodeRange EthiopicExtended
        {
            get { return UnicodeRanges._u2D80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2D80, 'ⶀ', '\x2DDF'); }
        }

        /// <summary>Gets the Cyrillic Extended-A Unicode block (U+2DE0-U+2DFF).</summary>
        /// <returns>The Cyrillic Extended-A Unicode block (U+2DE0-U+2DFF).</returns>
        public static UnicodeRange CyrillicExtendedA
        {
            get { return UnicodeRanges._u2DE0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2DE0, 'ⷠ', 'ⷿ'); }
        }

        /// <summary>Gets the Supplemental Punctuation Unicode block (U+2E00-U+2E7F).</summary>
        /// <returns>The Supplemental Punctuation Unicode block (U+2E00-U+2E7F).</returns>
        public static UnicodeRange SupplementalPunctuation
        {
            get { return UnicodeRanges._u2E00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2E00, '⸀', '\x2E7F'); }
        }

        /// <summary>Gets the CJK Radicals Supplement Unicode block (U+2E80-U+2EFF).</summary>
        /// <returns>The CJK Radicals Supplement Unicode block (U+2E80-U+2EFF).</returns>
        public static UnicodeRange CjkRadicalsSupplement
        {
            get { return UnicodeRanges._u2E80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2E80, '⺀', '\x2EFF'); }
        }

        /// <summary>Gets the Kangxi Radicals Supplement Unicode block (U+2F00-U+2FDF).</summary>
        /// <returns>The Kangxi Radicals Supplement Unicode block (U+2F00-U+2FDF).</returns>
        public static UnicodeRange KangxiRadicals
        {
            get { return UnicodeRanges._u2F00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2F00, '⼀', '\x2FDF'); }
        }

        /// <summary>Gets the Ideographic Description Characters Unicode block (U+2FF0-U+2FFF).</summary>
        /// <returns>The Ideographic Description Characters Unicode block (U+2FF0-U+2FFF).</returns>
        public static UnicodeRange IdeographicDescriptionCharacters
        {
            get { return UnicodeRanges._u2FF0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u2FF0, '⿰', '\x2FFF'); }
        }

        /// <summary>Gets the CJK Symbols and Punctuation Unicode block (U+3000-U+303F).</summary>
        /// <returns>The CJK Symbols and Punctuation Unicode block (U+3000-U+303F).</returns>
        public static UnicodeRange CjkSymbolsandPunctuation
        {
            get { return UnicodeRanges._u3000 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3000, '　', '〿'); }
        }

        /// <summary>Gets the Hiragana Unicode block (U+3040-U+309F).</summary>
        /// <returns>The Hiragana Unicode block (U+3040-U+309F).</returns>
        public static UnicodeRange Hiragana
        {
            get { return UnicodeRanges._u3040 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3040, '\x3040', 'ゟ'); }
        }

        /// <summary>Gets the Katakana Unicode block (U+30A0-U+30FF).</summary>
        /// <returns>The Katakana Unicode block (U+30A0-U+30FF).</returns>
        public static UnicodeRange Katakana
        {
            get { return UnicodeRanges._u30A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u30A0, '゠', 'ヿ'); }
        }

        /// <summary>Gets the Bopomofo Unicode block (U+3100-U+312F).</summary>
        /// <returns>The Bopomofo Unicode block (U+3105-U+312F).</returns>
        public static UnicodeRange Bopomofo
        {
            get
            {
                return UnicodeRanges._u3100 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3100, '\x3100', '\x312F');
            }
        }

        /// <summary>Gets the Hangul Compatibility Jamo Unicode block (U+3131-U+318F).</summary>
        /// <returns>The Hangul Compatibility Jamo Unicode block (U+3131-U+318F).</returns>
        public static UnicodeRange HangulCompatibilityJamo
        {
            get
            {
                return UnicodeRanges._u3130 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3130, '\x3130', '\x318F');
            }
        }

        /// <summary>Gets the Kanbun Unicode block (U+3190-U+319F).</summary>
        /// <returns>The Kanbun Unicode block (U+3190-U+319F).</returns>
        public static UnicodeRange Kanbun
        {
            get { return UnicodeRanges._u3190 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3190, '㆐', '㆟'); }
        }

        /// <summary>Gets the Bopomofo Extended Unicode block (U+31A0-U+31BF).</summary>
        /// <returns>The Bopomofo Extended Unicode block (U+31A0-U+31BF).</returns>
        public static UnicodeRange BopomofoExtended
        {
            get { return UnicodeRanges._u31A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u31A0, 'ㆠ', '\x31BF'); }
        }

        /// <summary>Gets the CJK Strokes Unicode block (U+31C0-U+31EF).</summary>
        /// <returns>The CJK Strokes Unicode block (U+31C0-U+31EF).</returns>
        public static UnicodeRange CjkStrokes
        {
            get { return UnicodeRanges._u31C0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u31C0, '㇀', '\x31EF'); }
        }

        /// <summary>Gets the Katakana Phonetic Extensions Unicode block (U+31F0-U+31FF).</summary>
        /// <returns>The Katakana Phonetic Extensions Unicode block (U+31F0-U+31FF).</returns>
        public static UnicodeRange KatakanaPhoneticExtensions
        {
            get { return UnicodeRanges._u31F0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u31F0, 'ㇰ', 'ㇿ'); }
        }

        /// <summary>Gets the Enclosed CJK Letters and Months Unicode block (U+3200-U+32FF).</summary>
        /// <returns>The Enclosed CJK Letters and Months Unicode block (U+3200-U+32FF).</returns>
        public static UnicodeRange EnclosedCjkLettersandMonths
        {
            get { return UnicodeRanges._u3200 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3200, '㈀', '\x32FF'); }
        }

        /// <summary>Gets the CJK Compatibility Unicode block (U+3300-U+33FF).</summary>
        /// <returns>The CJK Compatibility Unicode block (U+3300-U+33FF).</returns>
        public static UnicodeRange CjkCompatibility
        {
            get { return UnicodeRanges._u3300 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3300, '㌀', '㏿'); }
        }

        /// <summary>Gets the CJK Unitied Ideographs Extension A Unicode block (U+3400-U+4DB5).</summary>
        /// <returns>The CJK Unitied Ideographs Extension A Unicode block (U+3400-U+4DB5).</returns>
        public static UnicodeRange CjkUnifiedIdeographsExtensionA
        {
            get { return UnicodeRanges._u3400 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u3400, '㐀', '\x4DBF'); }
        }

        /// <summary>Gets the Yijing Hexagram Symbols Unicode block (U+4DC0-U+4DFF).</summary>
        /// <returns>The Yijing Hexagram Symbols Unicode block (U+4DC0-U+4DFF).</returns>
        public static UnicodeRange YijingHexagramSymbols
        {
            get { return UnicodeRanges._u4DC0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u4DC0, '䷀', '䷿'); }
        }

        /// <summary>Gets the CJK Unified Ideographs Unicode block (U+4E00-U+9FCC).</summary>
        /// <returns>The CJK Unified Ideographs Unicode block (U+4E00-U+9FCC).</returns>
        public static UnicodeRange CjkUnifiedIdeographs
        {
            get { return UnicodeRanges._u4E00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._u4E00, '一', '\x9FFF'); }
        }

        /// <summary>Gets the Yi Syllables Unicode block (U+A000-U+A48F).</summary>
        /// <returns>The Yi Syllables Unicode block (U+A000-U+A48F).</returns>
        public static UnicodeRange YiSyllables
        {
            get { return UnicodeRanges._uA000 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA000, 'ꀀ', '\xA48F'); }
        }

        /// <summary>Gets the Yi Radicals Unicode block (U+A490-U+A4CF).</summary>
        /// <returns>The Yi Radicals Unicode block (U+A490-U+A4CF).</returns>
        public static UnicodeRange YiRadicals
        {
            get { return UnicodeRanges._uA490 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA490, '꒐', '\xA4CF'); }
        }

        /// <summary>Gets the Lisu Unicode block (U+A4D0-U+A4FF).</summary>
        /// <returns>The Lisu Unicode block (U+A4D0-U+A4FF).</returns>
        public static UnicodeRange Lisu
        {
            get { return UnicodeRanges._uA4D0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA4D0, 'ꓐ', '꓿'); }
        }

        /// <summary>Gets the Vai Unicode block (U+A500-U+A63F).</summary>
        /// <returns>The Vai Unicode block (U+A500-U+A63F).</returns>
        public static UnicodeRange Vai
        {
            get { return UnicodeRanges._uA500 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA500, 'ꔀ', '\xA63F'); }
        }

        /// <summary>Gets the Cyrillic Extended-B Unicode block (U+A640-U+A69F).</summary>
        /// <returns>The Cyrillic Extended-B Unicode block (U+A640-U+A69F).</returns>
        public static UnicodeRange CyrillicExtendedB
        {
            get { return UnicodeRanges._uA640 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA640, 'Ꙁ', 'ꚟ'); }
        }

        /// <summary>Gets the Bamum Unicode block (U+A6A0-U+A6FF).</summary>
        /// <returns>The Bamum Unicode block (U+A6A0-U+A6FF).</returns>
        public static UnicodeRange Bamum
        {
            get { return UnicodeRanges._uA6A0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA6A0, 'ꚠ', '\xA6FF'); }
        }

        /// <summary>Gets the Modifier Tone Letters Unicode block (U+A700-U+A71F).</summary>
        /// <returns>The Modifier Tone Letters Unicode block (U+A700-U+A71F).</returns>
        public static UnicodeRange ModifierToneLetters
        {
            get { return UnicodeRanges._uA700 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA700, '꜀', 'ꜟ'); }
        }

        /// <summary>Gets the Latin Extended-D Unicode block (U+A720-U+A7FF).</summary>
        /// <returns>The Latin Extended-D Unicode block (U+A720-U+A7FF).</returns>
        public static UnicodeRange LatinExtendedD
        {
            get { return UnicodeRanges._uA720 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA720, '꜠', 'ꟿ'); }
        }

        /// <summary>Gets the Syloti Nagri Unicode block (U+A800-U+A82F).</summary>
        /// <returns>The Syloti Nagri Unicode block (U+A800-U+A82F).</returns>
        public static UnicodeRange SylotiNagri
        {
            get { return UnicodeRanges._uA800 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA800, 'ꠀ', '\xA82F'); }
        }

        /// <summary>Gets the Common Indic Number Forms Unicode block (U+A830-U+A83F).</summary>
        /// <returns>The Common Indic Number Forms Unicode block (U+A830-U+A83F).</returns>
        public static UnicodeRange CommonIndicNumberForms
        {
            get
            {
                return UnicodeRanges._uA830 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA830, '\xA830', '\xA83F');
            }
        }

        /// <summary>Gets the Phags-pa Unicode block (U+A840-U+A87F).</summary>
        /// <returns>The Phags-pa Unicode block (U+A840-U+A87F).</returns>
        public static UnicodeRange Phagspa
        {
            get { return UnicodeRanges._uA840 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA840, 'ꡀ', '\xA87F'); }
        }

        /// <summary>Gets the Saurashtra Unicode block (U+A880-U+A8DF).</summary>
        /// <returns>The Saurashtra Unicode block (U+A880-U+A8DF).</returns>
        public static UnicodeRange Saurashtra
        {
            get { return UnicodeRanges._uA880 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA880, 'ꢀ', '\xA8DF'); }
        }

        /// <summary>Gets the Devanagari Extended Unicode block (U+A8E0-U+A8FF).</summary>
        /// <returns>The Devanagari Extended Unicode block (U+A8E0-U+A8FF).</returns>
        public static UnicodeRange DevanagariExtended
        {
            get { return UnicodeRanges._uA8E0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA8E0, '꣠', '\xA8FF'); }
        }

        /// <summary>Gets the Kayah Li Unicode block (U+A900-U+A92F).</summary>
        /// <returns>The Kayah Li Unicode block (U+A900-U+A92F).</returns>
        public static UnicodeRange KayahLi
        {
            get { return UnicodeRanges._uA900 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA900, '꤀', '꤯'); }
        }

        /// <summary>Gets the Rejang Unicode block (U+A930-U+A95F).</summary>
        /// <returns>The Rejang Unicode block (U+A930-U+A95F).</returns>
        public static UnicodeRange Rejang
        {
            get { return UnicodeRanges._uA930 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA930, 'ꤰ', '꥟'); }
        }

        /// <summary>Gets the Hangul Jamo Extended-A Unicode block (U+A960-U+A9F).</summary>
        /// <returns>The Hangul Jamo Extended-A Unicode block (U+A960-U+A97F).</returns>
        public static UnicodeRange HangulJamoExtendedA
        {
            get { return UnicodeRanges._uA960 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA960, 'ꥠ', '\xA97F'); }
        }

        /// <summary>Gets the Javanese Unicode block (U+A980-U+A9DF).</summary>
        /// <returns>The Javanese Unicode block (U+A980-U+A9DF).</returns>
        public static UnicodeRange Javanese
        {
            get { return UnicodeRanges._uA980 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA980, 'ꦀ', '꧟'); }
        }

        /// <summary>Gets the Myanmar Extended-B Unicode block (U+A9E0-U+A9FF).</summary>
        /// <returns>The Myanmar Extended-B Unicode block (U+A9E0-U+A9FF).</returns>
        public static UnicodeRange MyanmarExtendedB
        {
            get { return UnicodeRanges._uA9E0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uA9E0, 'ꧠ', '\xA9FF'); }
        }

        /// <summary>Gets the Cham Unicode block (U+AA00-U+AA5F).</summary>
        /// <returns>The Cham Unicode block (U+AA00-U+AA5F).</returns>
        public static UnicodeRange Cham
        {
            get { return UnicodeRanges._uAA00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAA00, 'ꨀ', '꩟'); }
        }

        /// <summary>Gets the Myanmar Extended-A Unicode block (U+AA60-U+AA7F).</summary>
        /// <returns>The Myanmar Extended-A Unicode block (U+AA60-U+AA7F).</returns>
        public static UnicodeRange MyanmarExtendedA
        {
            get { return UnicodeRanges._uAA60 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAA60, 'ꩠ', 'ꩿ'); }
        }

        /// <summary>Gets the Tai Viet Unicode block (U+AA80-U+AADF).</summary>
        /// <returns>The Tai Viet Unicode block (U+AA80-U+AADF).</returns>
        public static UnicodeRange TaiViet
        {
            get { return UnicodeRanges._uAA80 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAA80, 'ꪀ', '꫟'); }
        }

        /// <summary>Gets the Meetei Mayek Extensions Unicode block (U+AAE0-U+AAFF).</summary>
        /// <returns>The Meetei Mayek Extensions Unicode block (U+AAE0-U+AAFF).</returns>
        public static UnicodeRange MeeteiMayekExtensions
        {
            get { return UnicodeRanges._uAAE0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAAE0, 'ꫠ', '\xAAFF'); }
        }

        /// <summary>Gets the Ethiopic Extended-A Unicode block (U+AB00-U+AB2F).</summary>
        /// <returns>The Ethiopic Extended-A Unicode block (U+AB00-U+AB2F).</returns>
        public static UnicodeRange EthiopicExtendedA
        {
            get
            {
                return UnicodeRanges._uAB00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAB00, '\xAB00', '\xAB2F');
            }
        }

        /// <summary>Gets the Latin Extended-E Unicode block (U+AB30-U+AB6F).</summary>
        /// <returns>The Latin Extended-E Unicode block (U+AB30-U+AB6F).</returns>
        public static UnicodeRange LatinExtendedE
        {
            get { return UnicodeRanges._uAB30 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAB30, 'ꬰ', '\xAB6F'); }
        }

        /// <summary>Gets the Cherokee Supplement Unicode block (U+AB70-U+ABBF).</summary>
        /// <returns>The Cherokee Supplement Unicode block (U+AB70-U+ABBF).</returns>
        public static UnicodeRange CherokeeSupplement
        {
            get { return UnicodeRanges._uAB70 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAB70, 'ꭰ', 'ꮿ'); }
        }

        /// <summary>Gets the Meetei Mayek Unicode block (U+ABC0-U+ABFF).</summary>
        /// <returns>The Meetei Mayek Unicode block (U+ABC0-U+ABFF).</returns>
        public static UnicodeRange MeeteiMayek
        {
            get { return UnicodeRanges._uABC0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uABC0, 'ꯀ', '\xABFF'); }
        }

        /// <summary>Gets the Hangul Syllables Unicode block (U+AC00-U+D7AF).</summary>
        /// <returns>The Hangul Syllables Unicode block (U+AC00-U+D7AF).</returns>
        public static UnicodeRange HangulSyllables
        {
            get { return UnicodeRanges._uAC00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uAC00, '가', '\xD7AF'); }
        }

        /// <summary>Gets the Hangul Jamo Extended-B Unicode block (U+D7B0-U+D7FF).</summary>
        /// <returns>The Hangul Jamo Extended-B Unicode block (U+D7B0-U+D7FF).</returns>
        public static UnicodeRange HangulJamoExtendedB
        {
            get { return UnicodeRanges._uD7B0 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uD7B0, 'ힰ', '\xD7FF'); }
        }

        /// <summary>Gets the CJK Compatibility Ideographs Unicode block (U+F900-U+FAD9).</summary>
        /// <returns>The CJK Compatibility Ideographs Unicode block (U+F900-U+FAD9).</returns>
        public static UnicodeRange CjkCompatibilityIdeographs
        {
            get { return UnicodeRanges._uF900 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uF900, '豈', '\xFAFF'); }
        }

        /// <summary>Gets the Alphabetic Presentation Forms Unicode block (U+FB00-U+FB4F).</summary>
        /// <returns>The Alphabetic Presentation Forms Unicode block (U+FB00-U+FB4F).</returns>
        public static UnicodeRange AlphabeticPresentationForms
        {
            get { return UnicodeRanges._uFB00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFB00, 'ﬀ', 'ﭏ'); }
        }

        /// <summary>Gets the Arabic Presentation Forms-A Unicode block (U+FB50-U+FDFF).</summary>
        /// <returns>The Arabic Presentation Forms-A Unicode block (U+FB50-U+FDFF).</returns>
        public static UnicodeRange ArabicPresentationFormsA
        {
            get { return UnicodeRanges._uFB50 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFB50, 'ﭐ', '\xFDFF'); }
        }

        /// <summary>Gets the Variation Selectors Unicode block (U+FE00-U+FE0F).</summary>
        /// <returns>The Variation Selectors Unicode block (U+FE00-U+FE0F).</returns>
        public static UnicodeRange VariationSelectors
        {
            get { return UnicodeRanges._uFE00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFE00, '︀', '️'); }
        }

        /// <summary>Gets the Vertical Forms Unicode block (U+FE10-U+FE1F).</summary>
        /// <returns>The Vertical Forms Unicode block (U+FE10-U+FE1F).</returns>
        public static UnicodeRange VerticalForms
        {
            get { return UnicodeRanges._uFE10 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFE10, '︐', '\xFE1F'); }
        }

        /// <summary>Gets the Combining Half Marks Unicode block (U+FE20-U+FE2F).</summary>
        /// <returns>The Combining Half Marks Unicode block (U+FE20-U+FE2F).</returns>
        public static UnicodeRange CombiningHalfMarks
        {
            get { return UnicodeRanges._uFE20 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFE20, '︠', '︯'); }
        }

        /// <summary>Gets the CJK Compatibility Forms Unicode block (U+FE30-U+FE4F).</summary>
        /// <returns>The CJK Compatibility Forms Unicode block (U+FE30-U+FE4F).</returns>
        public static UnicodeRange CjkCompatibilityForms
        {
            get { return UnicodeRanges._uFE30 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFE30, '︰', '﹏'); }
        }

        /// <summary>Gets the Small Form Variants Unicode block (U+FE50-U+FE6F).</summary>
        /// <returns>The Small Form Variants Unicode block (U+FE50-U+FE6F).</returns>
        public static UnicodeRange SmallFormVariants
        {
            get { return UnicodeRanges._uFE50 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFE50, '﹐', '\xFE6F'); }
        }

        /// <summary>Gets the Arabic Presentation Forms-B Unicode block (U+FE70-U+FEFF).</summary>
        /// <returns>The Arabic Presentation Forms-B Unicode block (U+FE70-U+FEFF).</returns>
        public static UnicodeRange ArabicPresentationFormsB
        {
            get { return UnicodeRanges._uFE70 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFE70, 'ﹰ', '\xFEFF'); }
        }

        /// <summary>Gets the Halfwidth and Fullwidth Forms Unicode block (U+FF00-U+FFEE).</summary>
        /// <returns>The Halfwidth and Fullwidth Forms Unicode block (U+FF00-U+FFEE).</returns>
        public static UnicodeRange HalfwidthandFullwidthForms
        {
            get
            {
                return UnicodeRanges._uFF00 ?? UnicodeRanges.CreateRange(ref UnicodeRanges._uFF00, '\xFF00', '\xFFEF');
            }
        }

        /// <summary>Gets the Specials Unicode block (U+FFF0-U+FFFF).</summary>
        /// <returns>The Specials Unicode block (U+FFF0-U+FFFF).</returns>
        public static UnicodeRange Specials
        {
            get
            {
                return UnicodeRanges._uFFF0 ??
                       UnicodeRanges.CreateRange(ref UnicodeRanges._uFFF0, '\xFFF0', char.MaxValue);
            }
        }
    }
}