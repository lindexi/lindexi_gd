using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using static CallnernawbawceKairwemwhejeene.UnicodeRanges;

namespace CallnernawbawceKairwemwhejeene
{
    class Program
    {
        static void Main(string[] args)
        {
            var latin = new CharUnicodeMerge();
            latin.Merge(BasicLatin,Latin1Supplement,LatinExtendedA,LatinExtendedB,IpaExtensions,GreekandCoptic,Cyrillic,CyrillicSupplement,Armenian);
            // 希伯来文 我不确定是否拉丁语系 亚非语系 闪语族 中闪米特语支 西北闪米特语支 迦南语支
            latin.Merge(Hebrew);
            // 阿拉伯语
            latin.Merge(Arabic,ArabicSupplement,ArabicExtendedA,ArabicPresentationFormsB);
            latin.Merge(Syriac,Thaana,NKo,Samaritan,Mandaic,SyriacSupplement,Devanagari,Bengali,Gurmukhi,Gujarati,Oriya,Tamil,Telugu,Kannada,Malayalam,Sinhala);

            var eastAsian = new CharUnicodeMerge();
            // 泰文 老挝文 藏文 缅甸文
            eastAsian.Merge(Thai,Lao,Tibetan,Myanmar);

            latin.Merge(Georgian);
            // 谚文字母 朝鲜字
            eastAsian.Merge(HangulJamo);
            //吉兹字母
            latin.Merge(Ethiopic,EthiopicSupplement,EthiopicExtended,EthiopicExtendedA,Cherokee,UnifiedCanadianAboriginalSyllabics,Ogham,UnifiedCanadianAboriginalSyllabicsExtended,Runic,Tagalog,Hanunoo,Buhid,Tagbanwa);

            //高棉文
            latin.Merge(Khmer);
            //蒙古
            eastAsian.Merge(Mongolian);
            latin.Merge(Limbu,TaiLe,NewTaiLue);

            var symbol = new CharUnicodeMerge();
            // 高棉文符号
            symbol.Merge(KhmerSymbols);

            latin.Merge(Buginese);
            // 老傣文
            eastAsian.Merge(TaiTham);
            // 组合变音标记扩展?
            latin.Merge(CombiningDiacriticalMarks,CombiningDiacriticalMarksExtended);
            // 巴厘字母
            latin.Merge(Balinese,Sundanese,Lepcha,OlChiki,CyrillicExtendedC,GeorgianExtended,SundaneseSupplement,VedicExtensions,PhoneticExtensions,PhoneticExtensionsSupplement,LatinExtendedA,LatinExtendedB,Latin1Supplement,LatinExtendedB,LatinExtendedC,LatinExtendedD,LatinExtendedE,GreekExtended,GreekandCoptic,GeneralPunctuation,SuperscriptsandSubscripts);
            // 货币符号
            symbol.Merge(CurrencySymbols,CombiningDiacriticalMarksforSymbols,LetterlikeSymbols);

            latin.Merge(NumberForms);
            // 箭头 数学运算符 这些是不是符号？
            latin.Merge(MiscellaneousTechnical);

            eastAsian.Merge(CjkRadicalsSupplement,CjkSymbolsandPunctuation,CjkStrokes,CjkCompatibility,CjkUnifiedIdeographs,CjkUnifiedIdeographsExtensionA,CjkCompatibilityIdeographs,CjkCompatibilityForms);
            eastAsian.Merge(Kanbun,KangxiRadicals,Hiragana,Katakana,HangulCompatibilityJamo,KatakanaPhoneticExtensions,EnclosedCjkLettersandMonths,YijingHexagramSymbols);

            //UTF-16的高半区
            symbol.Merge(new UnicodeRange(0xd800,0xdbff-0xd800+1),
                new UnicodeRange(0xDC00,0xDFFF-0xDC00+1));
            //私用区
            symbol.Merge(new UnicodeRange(0xE000,0xF8FF-0xE000+1));
            symbol.Merge(CombiningHalfMarks);


            Console.WriteLine(latin.ToString());
            Console.WriteLine();
            Console.WriteLine("ea");
            Console.WriteLine(eastAsian.ToString());
            Console.WriteLine();
            Console.WriteLine("symbol");
            Console.WriteLine(symbol.ToString());
        }
    }

    public class CharUnicodeMerge
    {
        public void Merge(params UnicodeRange[] unicodeRangeList)
        {
            foreach (var unicodeRange in unicodeRangeList)
            {
                Merge(unicodeRange);
            }
        }

        /// <summary>
        /// 合并多个平面的字符，这个方法只是在辅助提升性能，用于生成代码
        /// </summary>
        /// <param name="unicodeRange"></param>
        public void Merge(UnicodeRange unicodeRange)
        { 
            for (var i = 0; i < UnicodeRangeList.Count; i++)
            {
                var (name, range) = UnicodeRangeList[i];
                if (ReferenceEquals(unicodeRange, range))
                {
                    return;
                }

                if (range.FirstCodePoint < unicodeRange.FirstCodePoint)
                {
                    if (range.FirstCodePoint + range.Length + 1 >= unicodeRange.FirstCodePoint)
                    {
                        var end = unicodeRange.FirstCodePoint + unicodeRange.Length;

                        range = new UnicodeRange(range.FirstCodePoint, end - range.FirstCodePoint);
                        name += " " + CharUnicodeRange.GetUnicodeRangeName(unicodeRange);
                        UnicodeRangeList[i] = (name, range);

                        return;
                    }
                }

                if (range.FirstCodePoint > unicodeRange.FirstCodePoint)
                {
                    if (unicodeRange.FirstCodePoint + unicodeRange.Length + 1 >= range.FirstCodePoint)
                    {
                        var end = range.FirstCodePoint + range.Length;

                        range = new UnicodeRange(unicodeRange.FirstCodePoint, end - unicodeRange.FirstCodePoint);
                        name = CharUnicodeRange.GetUnicodeRangeName(unicodeRange) + " " + name;
                        UnicodeRangeList[i] = (name, range);
                    }
                }
            }

            UnicodeRangeList.Add((CharUnicodeRange.GetUnicodeRangeName(unicodeRange), unicodeRange));
        }

        private List<(string name, UnicodeRange unicodeRange)> UnicodeRangeList { get; } = new List<(string name, UnicodeRange unicodeRange)>();

        /// <inheritdoc />
        public override string ToString()
        {
            var str = new StringBuilder();
            foreach (var (name, unicodeRange) in UnicodeRangeList)
            {
                str.Append($"new UnicodeRange({unicodeRange.FirstCodePoint},{unicodeRange.Length}), // {name} \r\n");
            }

            return str.ToString();
        }
    }




}