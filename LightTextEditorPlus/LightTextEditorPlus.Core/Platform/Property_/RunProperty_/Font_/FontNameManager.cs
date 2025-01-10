using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 管理文本使用的字体名称和字体回退策略。
/// </summary>
public class FontNameManager : IFontNameManager
{
    ///// <summary>
    ///// 默认渲染字体Arial，用于缺失字体的渲染恢复，与微软机制一致
    ///// WPF 强依赖此字体，如果不存在，任何 WPF 程序都无法启动。
    ///// 所以文本模块可假设此字体一定存在，毕竟代码已经能跑到这里了。
    ///// </summary>
    //private const string FallbackDefaultFontName = "Arial";
    //private FontName? _defaultFontName;
    //private FontFamily? _defaultFontFamily;

    private readonly ConcurrentDictionary<string, string> _fallbackMapping = new();
    private readonly ConcurrentDictionary<string, string> _fuzzyFallbackMapping = new();
    private readonly ConcurrentDictionary<string/*FontName*/, FontFallbackInfo> _fallbackCache = new();

    /// <summary>
    /// 字体回退信息
    /// </summary>
    /// <param name="FallbackFontName">字体名</param>
    /// <param name="IsFallback">这个字体是否是回退的。False: 字体本身不需要回退</param>
    /// <param name="IsFallbackFailed">是否回退失败</param>
    readonly record struct FontFallbackInfo(string FallbackFontName, bool IsFallback, bool IsFallbackFailed);

    ///// <summary>
    ///// 获取默认的字体名。
    ///// </summary>
    ///// <remarks>
    ///// 请注意，默认的字体名不包含用户设置的字体。
    ///// </remarks>
    //public FontName DefaultFontName => _defaultFontName ??= new(DefaultFontFamily.Source);

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
    /// 字体回滚失败。每个字体最多只触发一次
    /// </summary>
    public event EventHandler<FontFallbackFailedEventArgs>? FontFallbackFailed;

    /// <summary>
    /// 获取字体回退策略
    /// </summary>
    /// <param name="desiredFontName"></param>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public string GetFallbackFontName(string desiredFontName, TextEditorCore textEditor)
    {
        IPlatformFontNameManager platformFontNameManager = textEditor.PlatformProvider.GetPlatformFontNameManager();

        var info = _fallbackCache.GetOrAdd(desiredFontName, k =>
        {
            var exactFontName = ExactSearch(k, platformFontNameManager);
            if (exactFontName is not null)
            {
                // 如果找到了精确匹配的字体，则直接返回
                // 如果找到的字体和期望的字体一致，则不是回退字体
                var isFallback = string.Equals(k, exactFontName, StringComparison.Ordinal);
                return new FontFallbackInfo(exactFontName, isFallback, IsFallbackFailed: false/*回退成功*/);
            }

            var fuzzyFontName = FuzzySearch(k, platformFontNameManager);
            if (fuzzyFontName is not null)
            {
                // 进入模糊匹配
                // 这个字体肯定是回退字体，即 IsFallback = true
                // 能回退成功，即 IsFallbackFailed = false
                return new FontFallbackInfo(fuzzyFontName, IsFallback: true, IsFallbackFailed: false);
            }

            FontFallbackFailed?.Invoke(this, new FontFallbackFailedEventArgs(k));

            // 如果回退失败，则使用默认字体
            string defaultFontName = platformFontNameManager.GetFallbackDefaultFontName();
            return new FontFallbackInfo(defaultFontName, IsFallback: true, IsFallbackFailed: true);
        });

        if (textEditor.IsInDebugMode)
        {
            if (info.IsFallback)
            {
                textEditor.Logger.LogDebug($"[FontNameManager] 触发字体回滚。原字体='{desiredFontName}'，回滚字体='info.FallbackFontName'，回滚失败={info.IsFallbackFailed}");
            }
        }

        return info.FallbackFontName;
    }

    /// <summary>
    /// 精确搜索字体
    /// </summary>
    /// <param name="desiredFontName"></param>
    /// <param name="platformFontNameManager"></param>
    /// <returns></returns>
    public string? ExactSearch(string desiredFontName, IPlatformFontNameManager platformFontNameManager)
    {
        var fontName = desiredFontName;
        int fallbackCount = 0;
        const int maxFallbackCount = 100;
        while (!platformFontNameManager.CheckFontFamilyInstalled(fontName))
        {
            // 如果 A->B B->A 则会陷入死循环
            fallbackCount++;
            if (fallbackCount > maxFallbackCount)
            {
                // 防止死循环
                return null;
            }

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

    /// <summary>
    /// 模糊搜索字体
    /// </summary>
    /// <param name="desiredFontName"></param>
    /// <param name="platformFontNameManager"></param>
    /// <returns></returns>
    public string? FuzzySearch(string desiredFontName, IPlatformFontNameManager platformFontNameManager)
    {
        foreach (var (key, value) in _fuzzyFallbackMapping)
        {
            if (desiredFontName.Contains(key))
            {
                return ExactSearch(value, platformFontNameManager);
            }
        }
        return null;
    }

    /// <summary>
    /// 使用默认的字体回退规则
    /// </summary>
    /// 这个方法由产品业务调用，默认不给开，不一定规则符合需求
    public void UseDefaultFontFallbackRules()
    {
        RegisterFontFallback(GetDefaultFallback());
        RegisterFuzzyFontFallback(GetFuzzyFallback());
    }

    /// <summary>
    /// 获取常用字体的回退策略。
    /// </summary>
    /// <remarks>
    /// 在默认情况下，Windows 系统会自带以下中文字体：
    /// <list type="bullet">
    /// <item>Windows 95：宋体、黑体、楷体_GB2312、仿宋_GB2312</item>
    /// <item>Windows XP：宋体/新宋体、黑体、楷体_GB2312、仿宋_GB2312、宋体-PUA</item>
    /// <item>Windows Vista：宋体/新宋体、黑体、楷体、仿宋、微软雅黑、SimSun-ExtB</item>
    /// <item>Windows 8：等线</item>
    /// </list>
    /// 以下字体为 Microsoft Office 自带：
    /// <list type="bullet">
    /// <item>Office：隶书、幼圆、方正舒体、方正姚体、华文细黑、华文楷体、华文宋体、华文中宋、华文仿宋、华文彩云、华文琥珀、华文隶书、华文行楷、华文新魏</item>
    /// </list>
    /// </remarks>
    /// <returns></returns>
    private static IDictionary<string, string> GetDefaultFallback() => new Dictionary<string, string>
    {
        // Windows 自带字体：考虑将 Office 字体和其他字体映射到这里
        { "等线", "微软雅黑" },
        { "等线 Light", "微软雅黑 Light" },
        { "仿宋", "宋体" },
        { "新宋体", "宋体" },
        { "楷体", "宋体" },
        { "微软雅黑", "黑体" },
        // Office 自带字体：考虑将其他字体映射到 Office 自带字体中
        { "隶书", "楷体" },
        { "幼圆", "黑体" },
        { "方正舒体", "楷体" },
        { "方正姚体", "仿宋" },
        { "华文细黑", "黑体" },
        { "华文楷体", "楷体" },
        { "华文宋体", "宋体" },
        { "华文中宋", "宋体" },
        { "华文仿宋", "仿宋" },
        { "华文彩云", "黑体" },
        { "华文琥珀", "黑体" },
        { "华文隶书", "楷体" },
        { "华文行楷", "楷体" },
        { "华文新魏", "楷体" },
        // 其他字体：考虑不要在其他字体范围内互相映射，而是映射成以上字体
        { "苹方", "微软雅黑" },
    };

    private static IDictionary<string, string> GetFuzzyFallback() => new Dictionary<string, string>
    {
        // 特殊艺术
        { "包图小白", "华文彩云" },
        { "方正喵呜", "黑体" },
        { "方正胖娃", "华文琥珀" },
        { "汉仪润圆", "楷体" },
        { "游圆", "幼圆" }, // 汉仪游圆
        { "正圆", "幼圆" }, // 汉仪正圆
        { "圆体", "幼圆" }, // 华康圆体
        { "篆书", "隶书" }, // 汉仪篆书
        { "钢笔体", "隶书" }, // 刻石录钢笔体
        // 类宋体
        { "造字工房尚雅", "宋体" },
        { "大漫漫体", "宋体" }, // 仓耳大漫漫体
        { "玄三", "宋体" }, // 仓耳玄三
        { "雅宋", "宋体" }, // 方正雅宋
        { "锐宋", "宋体" }, // 造字工房俊雅锐宋
        { "悦宋", "宋体" }, // 方正清刻本悦宋
        { "书宋", "宋体" }, // 方正书宋
        { "大宋", "宋体" }, // 汉仪大宋
        { "宋体", "宋体" }, // 思源宋体
        { "综艺", "宋体" }, // 方正综艺
        { "明体", "宋体" }, // 刻石录明体 源样明体
        { "仿宋", "仿宋" }, // 文悦古体仿宋
        // 类楷体
        { "行书", "楷体" },
        { "造字工房情书", "楷体" },
        { "静蕾简体", "楷体" },
        { "方正龙爪", "楷体" },
        { "方正萤雪", "楷体" },
        { "汉仪昌黎宋刻本", "楷体" },
        { "楷书", "楷体" },
        { "清楷", "楷体" },
        { "秀楷", "楷体" }, // 方正宋刻本秀楷
        { "柳楷", "楷体" }, // 方正苏新诗柳楷
        { "劲楷", "楷体" }, // 汉仪劲楷
        { "楷体", "楷体" },
        // 类黑体
        { "等线", "微软雅黑" },
        { "兰亭黑", "微软雅黑" }, // 方正兰亭黑 方正兰亭黑
        { "兰亭纤黑", "微软雅黑 Light" },
        { "兰亭超细黑", "微软雅黑 Light" },
        { "新青年", "黑体" }, // 文悦新青年体
        { "方圆", "黑体" }, // 汉仪方圆
        { "悦黑", "微软雅黑" }, // 造字工房悦黑
        { "正黑", "黑体" }, // 方正正黑
        { "旗黑", "黑体" }, // 汉仪旗黑
        { "酷黑", "黑体" }, // 汉仪雅酷黑 站酷酷黑
        { "高端黑", "黑体" }, // 站酷高端黑
        { "柔黑", "黑体" }, // 思源柔黑
        { "黑体", "黑体" }, // 冬青黑体 思源黑体
        // 类姚体
        { "方正非凡体", "方正姚体" },
        { "姚体", "方正姚体" },
        // 最后，用单字匹配，以适配变种字体名（如“仓耳今楷”、“方正雅宋”、“造字工房悦黑”）
        { "楷", "楷体" },
        { "宋", "宋体" },
        { "黑", "黑体" }, // 造字工房力黑
        { "圆", "幼圆" }, // 方正兰亭圆简体 腾祥沁圆

        //取消注释最后一行，可以修改回退未找到时候的默认值。
        //{ "", "微软雅黑" },
    };
}
