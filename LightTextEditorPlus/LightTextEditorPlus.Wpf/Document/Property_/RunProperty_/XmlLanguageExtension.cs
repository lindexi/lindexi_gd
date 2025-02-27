using System;
using System.Globalization;
using System.Windows.Markup;

namespace LightTextEditorPlus.Document
{
    /// <summary>
    /// 提供<see cref="XmlLanguage"/>比较的扩展方法
    /// </summary>
    static class XmlLanguageExtension
    {
        private const int MaxCultureDepth = 32;

        /// <summary>
        /// 判断指定<see cref="XmlLanguage"/>是否和指定<see cref="CultureInfo"/>兼容
        /// </summary>
        /// <param name="language"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static bool MatchCulture(this XmlLanguage? language, CultureInfo? culture)
        {
            // If there is no family map language, the family map applies to any language.
            if (language == null)
            {
                return true;
            }

            if (culture != null)
            {
                return language.RangeIncludes(culture);
            }

            return false;
        }

        /// <remarks>
        ///     Differs from calling string.StartsWith, because each subtag must match
        ///         in its entirety.
        ///     Note that this routine returns true if the tags match.
        /// </remarks>
        private static bool IsPrefixOf(this XmlLanguage language, string longTag)
        {
            string prefix = language.IetfLanguageTag;

            // if we fail a simple string-prefix test, we know we don't have a subtag-prefix.
            if (!longTag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // string-prefix test passed -- now determine if we're at a subtag-boundary
            return (prefix.Length == 0 || prefix.Length == longTag.Length || longTag[prefix.Length] == '-');
        }

        /// <summary>
        ///     Checks to see if a CultureInfo is included in range of languages specified
        ///       by this XmlLanguage.
        /// </summary>
        /// <remarks>
        ///    In addition to looking for prefix-matches in IetfLanguageTags, the implementation
        ///      also considers the Parent relationships among CultureInfo's.  So, in
        ///      particular, this routine will report that "zh-hk" is in the range specified by
        ///      "zh-hant", even though the latter is not a prefix of the former.   And because it
        ///      doesn't restrict itself to traversing CultureInfo.Parent, it will also report that
        ///      "sr-latn-sp" is in the range covered by "sr-latn".  (Note that "sr-latn" does
        ///      does not have a registered CultureInfo.)
        /// </remarks>
        internal static bool RangeIncludes(this XmlLanguage language, CultureInfo? culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            // no need for special cases for InvariantCulture, which has IetfLanguageTag == ""

            // Limit how far we'll walk up the hierarchy to avoid security threat.
            // We could check for cycles (e.g., culture.Parent.Parent == culture)
            // but in in the case of non-malicious code there should be no cycles,
            // whereas in malicious code, checking for cycles doesn't mitigate the
            // threat; one could always implement Parent such that it always returns
            // a new CultureInfo for which Equals always returns false.
            for (int i = 0; i < MaxCultureDepth; ++i)
            {
                // Note that we don't actually insist on a there being CultureInfo corresponding
                //  to language.
                // The use of language.StartsWith() catches, for example,the case
                //  where this="sr-latn", and culture.IetfLanguageTag=="sr-latn-sp".
                // In such a case, culture.Parent.IetfLanguageTag=="sr".
                //  (There is no registered CultureInfo with IetfLanguageTag=="sr-latn".)
                if (language.IsPrefixOf(culture.IetfLanguageTag))
                {
                    return true;
                }

                CultureInfo? parentCulture = culture.Parent;

                if (parentCulture == null
                    || parentCulture.Equals(CultureInfo.InvariantCulture)
                    || parentCulture == culture)
                    break;

                culture = parentCulture;
            }

            return false;
        }
    }
}