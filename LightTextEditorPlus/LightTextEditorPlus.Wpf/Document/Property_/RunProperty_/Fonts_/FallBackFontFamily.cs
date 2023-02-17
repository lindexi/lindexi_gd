using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace LightTextEditorPlus.Document
{
    /// <summary>
    /// 用于回滚的字体对象<see cref="FontFamily"/>
    /// </summary>
    class FallBackFontFamily
    {
#if LayoutOnly
        private FallBackFontFamily(CultureInfo culture)
        {
            // 不需要做什么，理论上仅布局不会用到这个类，因为此时不会存在找不到字体
        }
#else
        private const string FallBackFontFamilyName = "#GLOBAL USER INTERFACE";
        public FontFamily FallBack { get; } = new FontFamily(FallBackFontFamilyName);

        public FallBackFontFamily(CultureInfo culture)
        {
            FontFamilyItems = FallBack.FamilyMaps
                .Where(map => map.Language == null || XmlLanguageExtension.MatchCulture(map.Language, culture))
                .Select(map => new FontFamilyMapItem(map)).ToList();
        }

        private IReadOnlyList<FontFamilyMapItem> FontFamilyItems { get; }
#endif

        /// <summary>
        /// 获取<see cref="FallBackFontFamily"/>对象的单例
        /// </summary>
        public static FallBackFontFamily Instance => _lazy.Value;

        private static readonly Lazy<FallBackFontFamily> _lazy =
            new Lazy<FallBackFontFamily>(() => new FallBackFontFamily(CultureInfo.CurrentCulture));

        /// <summary>
        /// 尝试获取fallback的字体名称
        /// </summary>
        /// <param name="unicodeChar"></param>
        /// <param name="familyName"></param>
        /// <returns></returns>
      
        public bool TryGetFallBackFontFamily(char unicodeChar,[NotNullWhen(true)] out string? familyName)
        {
#if LayoutOnly
            familyName = TextContext.DefaultFontFamily.Source;
            return true;
#else
            var mapItem = GetFontFamilyMapItem(unicodeChar);
            familyName = null;

            if (mapItem != null)
            {
                familyName = mapItem.Target;
                return true;
            }

            return false;
#endif
        }

        public FontFamilyMapItem? GetFontFamilyMapItem(char unicodeChar)
        {
            var mapItem = FontFamilyItems.FirstOrDefault(item => item.InRange(unicodeChar));
            return mapItem;
        }
    }
}