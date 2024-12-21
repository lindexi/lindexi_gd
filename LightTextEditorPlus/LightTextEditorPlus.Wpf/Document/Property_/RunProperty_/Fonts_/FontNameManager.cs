using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Text; // todo 后续干掉 WinForms 的获取字体
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 字体管理器，提供判断已安装字体和回滚机制
/// </summary>
public class FontNameManager
{
    /// <summary>
    /// 默认渲染字体Arial，用于缺失字体的渲染恢复，与微软机制一致
    /// WPF 强依赖此字体，如果不存在，任何 WPF 程序都无法启动。
    /// 所以文本模块可假设此字体一定存在，毕竟代码已经能跑到这里了。
    /// </summary>
    public const string FallbackDefaultFontName = "Arial";
    //private FontFamily? _defaultFontFamily;

    private readonly ConcurrentDictionary<string, string> _fallbackMapping = new();
    private readonly ConcurrentDictionary<string, string> _fuzzyFallbackMapping = new();
    private readonly ConcurrentDictionary<string, string?> _fallbackCache = new();

    ///// <summary>
    ///// 获取默认的字体名。
    ///// </summary>
    ///// <remarks>
    ///// 请注意，默认的字体名不包含用户设置的字体。
    ///// </remarks>
    //public FontName DefaultFontName => new FontName(DefaultFontFamily.Source);

    ///// <summary>
    ///// 默认字体 微软雅黑
    ///// </summary>
    ///// <remarks>
    ///// 此值可被 <see cref="_fuzzyFallbackMapping"/> 覆盖。
    ///// 方法是调用 <see cref="RegisterFuzzyFontFallback(IDictionary{string, string})"/> 时指定一个空字符串作为 Key。
    ///// </remarks>
    //internal FontFamily DefaultFontFamily => _defaultFontFamily ??= new("Microsoft YaHei");

    /// <summary>
    /// 注册一条字体回退策略。
    /// 当 <paramref name="fontName"/> 字体未安装时，将使用 <paramref name="fallbackFontName"/> 字体。
    /// 如果后者也未安装，将继续根据相同规则查找。
    /// 最后均未找到时，将使用默认字体。
    /// </summary>
    /// <param name="fontName"></param>
    /// <param name="fallbackFontName"></param>
    public void RegisterFontFallback(string fontName, string fallbackFontName)
    {
        _fallbackMapping.AddOrUpdate(fontName, fallbackFontName, (k, v) => fallbackFontName);
    }

    /// <summary>
    /// 注册一条字体回退策略。
    /// 对于字典中的每一项当 Key 字体未安装时，将使用 Value 字体。
    /// 如果后者也未安装，将继续根据相同规则查找。
    /// 最后均未找到时，将使用默认字体。
    /// </summary>
    /// <param name="mapping"></param>
    public void RegisterFontFallback(IDictionary<string, string> mapping)
    {
        foreach (var (fontName, fallbackFontName) in mapping)
        {
            _fallbackMapping.AddOrUpdate(fontName, fallbackFontName, (k, v) => fallbackFontName);
        }
    }

    /// <summary>
    /// 注册一条模糊字体回退策略。
    /// 当某字体未安装但能与 <paramref name="fuzzyFontName"/> 模糊匹配时，将使用 <paramref name="fallbackFontName"/> 字体。
    /// 如果后者也未安装，将继续使用精确匹配方式回退。
    /// 最后均未找到时，将使用默认字体。
    /// </summary>
    /// <param name="fuzzyFontName"></param>
    /// <param name="fallbackFontName"></param>
    public void RegisterFuzzyFontFallback(string fuzzyFontName, string fallbackFontName)
    {
        _fuzzyFallbackMapping.AddOrUpdate(fuzzyFontName, fallbackFontName, (k, v) => fallbackFontName);
    }

    /// <summary>
    /// 注册一条字体回退策略。
    /// 对于字典中的每一项，当某字体未安装但能与 Key 模糊匹配时，将使用 Value 字体。
    /// 如果后者也未安装，将继续使用精确匹配方式回退。
    /// 最后均未找到时，将使用默认字体。
    /// </summary>
    /// <param name="mapping"></param>
    public void RegisterFuzzyFontFallback(IDictionary<string, string> mapping)
    {
        foreach (var (fontName, fallbackFontName) in mapping)
        {
            _fuzzyFallbackMapping.AddOrUpdate(fontName, fallbackFontName, (k, v) => fallbackFontName);
        }
    }

    /// <summary>
    /// 字体回滚失败
    /// </summary>
    public event EventHandler<FontFallbackFailedEventArgs>? FontFallbackFailed;

    internal string? GetFallbackFontName(string desiredFontName)
    {
        return _fallbackCache.GetOrAdd(desiredFontName, k =>
        {
            var exactFontName = ExactSearch(k);
            if (exactFontName is not null)
            {
                return exactFontName;
            }

            var fuzzyFontName = FuzzySearch(k);
            if (fuzzyFontName is not null)
            {
                return fuzzyFontName;
            }

            FontFallbackFailed?.Invoke(this, new FontFallbackFailedEventArgs(k));

            // 返回找不到字体
            return null;
        });
    }

    internal string? ExactSearch(string desiredFontName)
    {
        var fontName = desiredFontName;
        while (!CheckFontFamilyInstalled(fontName))
        {
            if (_fallbackMapping.TryGetValue(fontName, out var fallback))
            {
                // 如果发现回退策略，则继续检查回退字体是否可用。
                fontName = fallback;
            }
            else
            {
                // 如果未发现回退策略，则返回默认字体。
                return null;
            }
        }
        return fontName;
    }

    internal string? FuzzySearch(string desiredFontName)
    {
        foreach (var (key, value) in _fuzzyFallbackMapping)
        {
            if (desiredFontName.Contains(key))
            {
                return ExactSearch(value);
            }
        }
        return null;
    }

    /// <summary>
    /// 已安装的字体列表
    /// </summary>
    public static List<string> InstalledFontFamiliesEx =>
        _installedFontFamiliesEx ??= GetInstalledFamiliesEx();

    /// <summary>
    /// 判断字体是否已经安装，里面使用 HASH 方法，性能比较好
    /// </summary>
    /// <param name="fontFamilySource"></param>
    /// <returns></returns>
    public static bool CheckFontFamilyInstalled(string fontFamilySource)
    {
        return InstalledFontFamiliesHashSet.Contains(fontFamilySource);
    }

    private static List<string>? _installedFontFamiliesEx;

    private static HashSet<string> InstalledFontFamiliesHashSet => _installedFontFamiliesHashSet ??= new HashSet<string>(InstalledFontFamiliesEx);

    private static HashSet<string>? _installedFontFamiliesHashSet;

    #region 静态

    /// <summary>
    /// 获取当前机器上的字体名称，会返回所有Culture下的名称
    /// </summary>
    /// <returns></returns>
    private static List<string> GetInstalledFamiliesEx()
    {
        //zh - CN 0x0804 中文 - 中国
        //zh - CHS 0x0004 中文（简体）
        //en - GB 0x0809 英语 - 英国
        //en - US 0x0409 英语 - 美国
        const int USCode = 0x0409;
        //ja - JP 0x0411日语 - 日本
        var fontCollection = GetInstalledFontFamilies();

        var familyNames = fontCollection.Select(x => x.Name).Where(x => !string.IsNullOrEmpty(x)).ToList();
        var englishNames = fontCollection.Select(x => x.GetName(USCode))
            .Where(x => !string.IsNullOrEmpty(x));

        foreach (var englishName in englishNames)
        {
            if (!familyNames.Contains(englishName))
            {
                familyNames.Add(englishName);
            }
        }

        //通过所有Culture下的字体名称比对，来处理不同系统下打开后依然能够正常判断
        //但是，有的字体在设计名称时存在问题，比如“微软雅黑 light”
        //中文情况下，它的字体名称是“微软雅黑 Light”，中文和英文名称分别是“微软雅黑”“Microsoft YaHei”
        //英文情况下，它的字体名称是“Microsoft YaHei Light”，中文和英文名称分别是“微软雅黑”“Microsoft YaHei”
        //这就导致在中文环境下设置的字体在英文环境下无法识别，将被回退为Arail
        //如果要从根本上解决此问题，需要在序列化的时候考虑Culture
        var familyNamesWithAllCulture = new List<string>();
        familyNamesWithAllCulture.AddRange(familyNames);
        foreach (var familyName in familyNames)
        {
            try
            {
                var f = new FontFamily(familyName);
                if (f.FamilyNames.Values is not null)
                {
                    familyNamesWithAllCulture.AddRange(f.FamilyNames.Values.ToList());
                }
                else
                {
                    familyNamesWithAllCulture.Add(familyName);
                }
            }
            catch (Exception)
            {
                // 获取当前机器上的字体名称失败
            }
        }

        familyNamesWithAllCulture.AddRange(GetSystemFontFamilies());
        return familyNamesWithAllCulture.Distinct().ToList();
    }

    private static List<string> GetSystemFontFamilies()
    {
        try
        {
            //WPF拿取区域字体
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            var defaultCulture = new CultureInfo("en-US");

            //添加本地区域字体
            var familiesNames = Fonts.SystemFontFamilies.Select(x => x.FamilyNames).Select(x => x.FirstOrDefault(k => Equals(k.Key.GetSpecificCulture(), currentCulture))).Where(x => x.Key != null).Select(x => x.Value).ToList();
            familiesNames.Sort();

            //添加默认区域字体
            if (!Equals(currentCulture, defaultCulture))
            {
                var defaultFonts = Fonts.SystemFontFamilies.Select(x => x.FamilyNames).Where(x => x.Any(k => Equals(k.Key.GetSpecificCulture(), defaultCulture)) && x.All(k => !Equals(k.Key.GetSpecificCulture(), currentCulture))).Select(x => x.FirstOrDefault(k => Equals(k.Key.GetSpecificCulture(), defaultCulture))).Where(x => x.Key != null).Select(x => x.Value).ToList();
                defaultFonts.Sort();
                familiesNames.AddRange(defaultFonts);
            }
            return familiesNames.ToList();
        }
        //Fonts.SystemFontFamilies可能会抛出异常
        catch (Exception)
        {
            return new List<string>(0);
        }
    }

    private static List<System.Drawing.FontFamily> GetInstalledFontFamilies()
    {
        try
        {
            var fc = new InstalledFontCollection();
            return fc.Families.ToList();
        }
        catch (Exception)
        {
            return new List<System.Drawing.FontFamily>(0);
        }
    }

    #endregion
}

/// <summary>
/// 字体回滚失败的事件参数
/// </summary>
public class FontFallbackFailedEventArgs : EventArgs
{
    /// <summary>
    /// 创建字体回滚失败的事件参数
    /// </summary>
    /// <param name="fontName"></param>
    public FontFallbackFailedEventArgs(string fontName)
    {
        FontName = fontName;
    }

    /// <summary>
    /// 回滚失败的字体名
    /// </summary>
    public string FontName { get; }
}