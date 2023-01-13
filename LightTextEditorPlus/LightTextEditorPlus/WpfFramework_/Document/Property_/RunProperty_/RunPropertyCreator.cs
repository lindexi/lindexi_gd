using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Document;

class RunPropertyCreator : PlatformRunPropertyCreatorBase<RunProperty>
{
    public RunPropertyCreator()
    {
    }

    protected override RunProperty OnBuildNewProperty(Action<IReadOnlyRunProperty> config, RunProperty baseRunProperty)
    {
        var runProperty = new RunProperty(_runPropertyPlatformManager, baseRunProperty);
        config(runProperty);
        return runProperty;
    }

    protected override RunProperty OnGetDefaultRunProperty()
    {
        return new RunProperty(_runPropertyPlatformManager);
    }

    private readonly RunPropertyPlatformManager _runPropertyPlatformManager = new RunPropertyPlatformManager();
}

/// <summary>
/// 用来管理本地资源，包括画刷或者是字体等
/// </summary>
class RunPropertyPlatformManager
{
    /// <summary>
    /// 判断两个字符属性是否有相等字体
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public bool IsSameTypeface(RunProperty a, RunProperty b)
    {

    }

    public GlyphTypeface GetGlyphTypeface(RunProperty runProperty)
    {
        // todo 字体回滚，字体缓存
        FontFamily fontFamily;
        if (runProperty.FontName.IsNotDefineFontName)
        {
            fontFamily = new FontFamily("微软雅黑");
        }
        else
        {
            fontFamily = new FontFamily(runProperty.FontName.UserFontName);
        }

        var collection = fontFamily.GetTypefaces();
        Typeface typeface = collection.First();

        foreach (var t in collection)
        {
            if (t.Stretch == runProperty.Stretch && t.Weight == runProperty.Weight)
            {
                typeface = t;
                break;
            }
        }

        bool success = typeface.TryGetGlyphTypeface(out var glyphTypeface);
        return glyphTypeface;
    }
}