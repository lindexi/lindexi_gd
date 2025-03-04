using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document
{
    /// <summary>
    /// 用于回滚的字体对象<see cref="FontFamily"/>
    /// </summary>
    class FallBackFontFamily
    {
        private const string FallBackFontFamilyName = "#GLOBAL USER INTERFACE";
        public FontFamily FallBack { get; } = new FontFamily(FallBackFontFamilyName);

        public FallBackFontFamily(CultureInfo culture)
        {
            FontFamilyItems = FallBack.FamilyMaps
                .Where(map => map.Language == null || XmlLanguageExtension.MatchCulture(map.Language, culture))
                .Select(map => new FontFamilyMapItem(map)).ToList();
        }

        private IReadOnlyList<FontFamilyMapItem> FontFamilyItems { get; }

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

        public bool TryGetFallBackFontFamily(Utf32CodePoint unicodeChar, [NotNullWhen(true)] out string? familyName)
        {
            var mapItem = GetFontFamilyMapItem(unicodeChar);
            familyName = null;

            if (mapItem != null)
            {
                familyName = mapItem.Target;
                return true;
            }

            return false;
        }

        public FontFamilyMapItem? GetFontFamilyMapItem(Utf32CodePoint unicodeChar)
        {
            var mapItem = FontFamilyItems.FirstOrDefault(item => item.InRange(unicodeChar.Value));
            return mapItem;
        }
    }
}
