using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Threading;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Layout;
using LightTextEditorPlus.TextEditorPlus.Render;
using LightTextEditorPlus.Utils.Threading;

using Microsoft.Win32;

using Point = LightTextEditorPlus.Core.Primitive.Point;
using Rect = LightTextEditorPlus.Core.Primitive.Rect;
using Size = LightTextEditorPlus.Core.Primitive.Size;

namespace LightTextEditorPlus;

public partial class TextEditor : FrameworkElement, IRenderManager
{
    public TextEditor()
    {
        TextView = new TextView(this);
        // 加入视觉树，方便调试和方便触发视觉变更
        AddVisualChild(TextView);
        AddLogicalChild(TextView);

        #region 清晰文本

        SnapsToDevicePixels = true;
        RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        #endregion

        #region 配置文本

        var textEditorPlatformProvider = new TextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(textEditorPlatformProvider);
        SetDefaultTextRunProperty(property =>
        {
            property.FontSize = 30;
        });

        TextEditorPlatformProvider = textEditorPlatformProvider;

        #endregion

        Loaded += TextEditor_Loaded;
    }

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
    }

    #region 公开属性

    public TextEditorCore TextEditorCore { get; }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置当前文本的默认字符属性
    /// </summary>
    public void SetDefaultTextRunProperty(Action<RunProperty> config)
    {
        TextEditorCore.DocumentManager.SetDefaultTextRunProperty<RunProperty>(config);
    }

    /// <summary>
    /// 设置当前光标的字符属性。在光标切走之后，自动失效
    /// </summary>
    public void SetCurrentCaretRunProperty(Action<RunProperty> config)
        => TextEditorCore.DocumentManager.SetCurrentCaretRunProperty<RunProperty>(config);

    public void SetRunProperty(Action<RunProperty> config, Selection? selection = null)
        => TextEditorCore.DocumentManager.SetRunProperty(config, selection);

    #endregion

    #region 框架
    /// <summary>
    /// 视觉呈现容器
    /// </summary>
    private TextView TextView { get; }
    protected override int VisualChildrenCount => 1; // 当前只有视觉呈现容器一个而已
    protected override Visual GetVisualChild(int index) => TextView;

    internal TextEditorPlatformProvider TextEditorPlatformProvider { get; }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        TextView.Render(renderInfoProvider);
    }

    #endregion
}

internal class TextEditorPlatformProvider : PlatformProvider
{
    public TextEditorPlatformProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;

        _textLayoutDispatcherRequiring = new DispatcherRequiring(UpdateLayout, DispatcherPriority.Render);
        _charInfoMeasurer = new CharInfoMeasurer(textEditor);
        _runPropertyCreator = new RunPropertyCreator();
    }

    private void UpdateLayout()
    {
        Debug.Assert(_lastTextLayout is not null);
        _lastTextLayout?.Invoke();
    }

    private TextEditor TextEditor { get; }
    private readonly DispatcherRequiring _textLayoutDispatcherRequiring;
    private Action? _lastTextLayout;

    public override void RequireDispatchUpdateLayout(Action textLayout)
    {
        _lastTextLayout = textLayout;
        _textLayoutDispatcherRequiring.Require();
    }

    public override ICharInfoMeasurer? GetCharInfoMeasurer()
    {
        return _charInfoMeasurer;
    }

    private readonly CharInfoMeasurer _charInfoMeasurer;

    public override IRenderManager? GetRenderManager()
    {
        return TextEditor;
    }

    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator() => _runPropertyCreator;

    private readonly RunPropertyCreator _runPropertyCreator; //= new RunPropertyCreator();
}

class CharInfoMeasurer : ICharInfoMeasurer
{
    public CharInfoMeasurer(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }
    private readonly TextEditor _textEditor;

    // todo 允许开发者设置默认字体
    private readonly FontFamily _defaultFontFamily = new FontFamily($"微软雅黑");

    public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
    {
        // todo 属性系统需要加上字体管理模块
        // todo 处理字体回滚
        var runPropertyFontFamily = charInfo.RunProperty.AsRunProperty().FontName;
        var fontFamily = ToFontFamily(runPropertyFontFamily);
        var collection = fontFamily.GetTypefaces();
        Typeface typeface = collection.First();
        foreach (var t in collection)
        {
            if (t.Stretch == FontStretches.Normal && t.Weight == FontWeights.Normal)
            {
                typeface = t;
                break;
            }
        }

        bool success = typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface);

        if (!success)
        {
            // 处理字体回滚
        }

        // todo 对于字符来说，反复在字符串和字符转换，需要优化
        var text = charInfo.CharObject.ToText();

        var size = Size.Zero;

        if (_textEditor.TextEditorCore.ArrangingType == ArrangingType.Horizontal)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                var glyphIndex = glyphTypeface.CharacterToGlyphMap[c];

                var fontSize = charInfo.RunProperty.FontSize;

                var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                width = GlyphExtension.RefineValue(width);
                var height = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

                size = size.HorizontalUnion(width, height);
            }
        }
        else
        {
            throw new NotImplementedException("还没有实现竖排的文本测量");
        }

        return new CharInfoMeasureResult(new Rect(new Point(), size));
    }

    private FontFamily ToFontFamily(FontName runPropertyFontFamily)
    {
        if (runPropertyFontFamily.IsNotDefineFontName)
        {
            // 如果采用没有定义的字体名，那就返回默认的字体
            return _defaultFontFamily;
        }

        var fontFamily = new FontFamily(runPropertyFontFamily.UserFontName);
        return fontFamily;
    }
}