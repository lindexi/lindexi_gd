using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace HerjemhokiNeakunegar
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures))
            {
                Console.WriteLine($" {{\"{cultureInfo.Name}\",\"{cultureInfo.EnglishName}\"}},");
            }
        }
    }
}

/*
        private static readonly Dictionary<string, string> LanguageScriptTagDictionary =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"ja-JP", "Jpan"},
                {"ko-KR", "Hang"},
                {"zh-CN", "Hans"},
                {"zh-Hans", "Hans"},
                {"ar", "Arab"}, // {"ar-AE","Arabic (United Arab Emirates)"},
                {"ar-001", "Arab"}, // {"ar-001","Arabic (World)"},
                {"ar-AE", "Arab"}, // {"ar-AE","Arabic (United Arab Emirates)"},
                {"ar-BH", "Arab"}, //"Arabic (Bahrain)"},
                {"ar-DJ", "Arab"}, //"Arabic (Djibouti)"},
                {"ar-DZ", "Arab"}, //"Arabic (Algeria)"},
                {"ar-EG", "Arab"}, //"Arabic (Egypt)"},
                {"ar-ER", "Arab"}, //"Arabic (Eritrea)"},
                {"ar-IL", "Arab"}, //"Arabic (Israel)"},
                {"ar-IQ", "Arab"}, //"Arabic (Iraq)"},
                {"ar-JO", "Arab"}, //"Arabic (Jordan)"},
                {"ar-KM", "Arab"}, //"Arabic (Comoros)"},
                {"ar-KW", "Arab"}, //"Arabic (Kuwait)"},
                {"ar-LB", "Arab"}, //"Arabic (Lebanon)"},
                {"ar-LY", "Arab"}, //"Arabic (Libya)"},
                {"ar-MA", "Arab"}, //"Arabic (Morocco)"},
                {"ar-MR", "Arab"}, //"Arabic (Mauritania)"},
                {"ar-OM", "Arab"}, //"Arabic (Oman)"},
                {"ar-PS", "Arab"}, //"Arabic (Palestinian Authority)"},
                {"ar-QA", "Arab"}, //"Arabic (Qatar)"},
                {"ar-SA", "Arab"}, //"Arabic (Saudi Arabia)"},
                {"ar-SD", "Arab"}, //"Arabic (Sudan)"},
                {"ar-SO", "Arab"}, //"Arabic (Somalia)"},
                {"ar-SS", "Arab"}, //"Arabic (South Sudan)"},
                {"ar-SY", "Arab"}, //"Arabic (Syria)"},
                {"ar-TD", "Arab"}, //"Arabic (Chad)"},
                {"ar-TN", "Arab"}, //"Arabic (Tunisia)"},
                {"ar-YE", "Arab"}, //"Arabic (Yemen)"},
                {"he", "Hebr"}, // {"he","Hebrew"},
                {"he-IL", "Hebr"}, // {"he-IL","Hebrew (Israel)"},
                {"th", "Thai"}, // {"th","Thai"},
                {"th-TH", "Thai"}, // {"th-TH","Thai (Thailand)"},
                {"aa-ET", "Ethi"}, // {"am-ET","Amharic (Ethiopia)"},
                {"om-ET", "Ethi"}, // {"om-ET","Oromo (Ethiopia)"},
                {"so-ET", "Ethi"}, // {"so-ET","Somali (Ethiopia)"},
                {"ti-ET", "Ethi"}, // {"ti-ET","Tigrinya (Ethiopia)"},
                {"wal-ET", "Ethi"}, // {"wal-ET","Wolaytta (Ethiopia)"},
                {"bn-IN", "Beng"}, //{"bn-IN","Bengali (India)"},
                //{"","Gujr"}, // 没有找到对应的语言
                {
                    "km", "Khmr"
                }, // {"km","Khmer"}, [Khmer language - Wikipedia](https://en.wikipedia.org/wiki/Khmer_language )
                {"km-KH", "Khmr"}, // {"km-KH","Khmer (Cambodia)"},
                {
                    "tcy-Knda", "Knda"
                }, // [Template:ISO 639 name tcy-Knda - Wikipedia](https://en.wikipedia.org/wiki/Template:ISO_639_name_tcy-Knda )
                {"pa-Guru", "Guru"}, // {"pa-Guru","Punjabi"},
                {"iu-Cans", "Cans"}, // {"iu-Cans","Inuktitut (Syllabics)"},
                {"iu-Cans-CA", "Cans"}, // {"iu-Cans-CA","Inuktitut (Syllabics, Canada)"},
                {"chr", "Cher"}, // {"chr","Cherokee"},
                {"chr-Cher", "Cher"}, // {"chr-Cher","Cherokee"},
                {"chr-Cher-US", "Cher"}, // {"chr-Cher-US","Cherokee (Cherokee, United States)"},
                //{"","Yiii"},// 没有找到对应的语言
                {
                    "bo", "Tibt"
                }, // {"bo","Tibetan"}, [Standard Tibetan - Wikipedia](https://en.wikipedia.org/wiki/Standard_Tibetan )
                {"bo-CN", "Tibt"}, // {"bo-CN","Tibetan (China)"},
                {"bo-IN", "Tibt"}, // {"bo-IN","Tibetan (India)"},
                //{"","Thaa"},// 没有找到对应的语言 [Thaana - Wikipedia](https://en.wikipedia.org/wiki/Thaana ) Unicode range U+0780–U+07BF
                {"ks-Deva", "Deva"}, // {"ks-Deva","Kashmiri (Devanagari)"},
                {"ks-Deva-IN", "Deva"}, // {"ks-Deva-IN","Kashmiri (Devanagari)"},
                {"sd-Deva", "Deva"}, // {"sd-Deva","Sindhi (Devanagari)"},
                {"sd-Deva-IN", "Deva"}, // {"sd-Deva-IN","Sindhi (Devanagari, India)"},
                {"te", "Telu"}, // {"te","Telugu"},
                {"te-IN", "Telu"}, // {"te-IN","Telugu (India)"},
                {"ta", "Taml"}, // {"ta","Tamil"},
                {"ta-IN", "Taml"}, // {"ta-IN","Tamil (India)"},
                {"ta-LK", "Taml"}, // {"ta-LK","Tamil (Sri Lanka)"},
                {"ta-MY", "Taml"}, // {"ta-MY","Tamil (Malaysia)"},
                {"ta-SG", "Taml"}, // {"ta-SG","Tamil (Singapore)"},
                {
                    "syr", "Syrc"
                }, // {"syr","Syriac"}, [Syriac language - Wikipedia](https://en.wikipedia.org/wiki/Syriac_language )
                {"syr-SY", "Syrc"}, // {"syr-SY","Syriac (Syria)"},
                //{"","Orya"},// [Orya language - Wikipedia](https://en.wikipedia.org/wiki/Orya_language )
                {
                    "ml", "Mlym"
                }, // {"ml","Malayalam"}, [Malayalam script - Wikipedia](https://en.wikipedia.org/wiki/Malayalam_script )
                {"ml-IN", "Mlym"}, // {"ml-IN","Malayalam (India)"},
                {"lo", "Laoo"}, // {"lo","Lao"}, [Lao language - Wikipedia](https://en.wikipedia.org/wiki/Lao_language )
                {"lo-LA", "Laoo"}, // 寮語 {"lo-LA","Lao (Laos)"},
                {"si", "Sinh"}, // {"si","Sinhala"},
                {"si-LK", "Sinh"}, // {"si-LK","Sinhala (Sri Lanka)"},
                {"mn", "Mong"}, // {"mn","Mongolian"},
                {"mn-Cyrl", "Mong"}, // {"mn-Cyrl","Mongolian"},
                {"mn-MN", "Mong"}, // {"mn-MN","Mongolian (Mongolia)"},
                {"mn-Mong", "Mong"}, // {"mn-Mong","Mongolian (Traditional Mongolian)"},
                {"mn-Mong-CN", "Mong"}, // {"mn-Mong-CN","Mongolian (Traditional Mongolian, China)"},
                {"mn-Mong-MN", "Mong"}, // {"mn-Mong-MN","Mongolian (Traditional Mongolian, Mongolia)"},
                {"vi", "Viet"}, // {"vi","Vietnamese"},
                {"vi-VN", "Viet"}, // {"vi-VN","Vietnamese (Vietnam)"},
                //{"","Uigh"},// 没有找到对应的语言 [Uyghur language - Wikipedia](https://en.wikipedia.org/wiki/Uyghur_language )
                {"ka", "Geor"}, // {"ka","Georgian"},
                {"ka-GE", "Geor"}, // {"ka-GE","Georgian (Georgia)"},
                {"os-GE", "Geor"}, // {"os-GE","Ossetic (Georgia)"},
            };
 */
