using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class FontNameManagerTest
{
    [UIContractTestCase]
    public void RegisterFontFallback()
    {
        "对文本进行注册字体回滚，可以成功注册".Test(() =>
        {
            TextEditor.StaticConfiguration.FontNameManager.RegisterFontFallback(GetDefaultFallback());
            TextEditor.StaticConfiguration.FontNameManager.RegisterFuzzyFontFallback(GetFuzzyFallback());
            // 没有抛异常就是成功
        });

        "如果字体找不到回滚，将会触发字体回滚失败事件".Test(() =>
        {
            const string fontName = "一个不存在的字体xxxxasdasd";
            var count = 0;

            TextEditor.StaticConfiguration.FontNameManager.FontFallbackFailed += (sender, args) =>
            {
                if (args.FontName.Equals(fontName, StringComparison.Ordinal))
                {
                    count++;
                }
            };

            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();
            var styleRunProperty = runPropertyCreator.GetDefaultRunProperty().AsRunProperty() with
            {
                FontName = new FontName(fontName)
            };



            styleRunProperty.GetGlyphTypeface();

            Assert.AreEqual(true, count > 0);
        });
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