using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.TextEditorPlus.Render;

using Microsoft.Win32;

using Point = LightTextEditorPlus.Core.Primitive.Point;
using Rect = LightTextEditorPlus.Core.Primitive.Rect;
using Size = LightTextEditorPlus.Core.Primitive.Size;

namespace LightTextEditorPlus;

public partial class TextEditor : FrameworkElement, IRenderManager
{
    public TextEditor()
    {
        var textEditorPlatformProvider = new TextEditorPlatformProvider(this);
        TextEditorCore = new TextEditorCore(textEditorPlatformProvider);
        TextEditorPlatformProvider = textEditorPlatformProvider;

        SnapsToDevicePixels = true;
        RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        Loaded += TextEditor_Loaded;
    }

    private void TextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        // 这是测试代码 todo 删除测试代码
        TextEditorCore.AppendText("123gf");
    }

    public TextEditorCore TextEditorCore { get; }
    internal TextEditorPlatformProvider TextEditorPlatformProvider { get; }

    private readonly DrawingGroup _drawingGroup = new DrawingGroup();

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawDrawing(_drawingGroup);
    }

    void IRenderManager.Render(RenderInfoProvider renderInfoProvider)
    {
        var pixelsPerDip = (float) VisualTreeHelper.GetDpi(this).PixelsPerDip;

        using (var drawingContext = _drawingGroup.Open())
        {
            foreach (var paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
            {
                foreach (var lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
                {
                    var argument = lineRenderInfo.Argument;
                    foreach (var charData in argument.CharList)
                    {
                        var startPoint = charData.GetStartPoint();

                        var fontFamily = new FontFamily("微软雅黑"); //ToFontFamily(runPropertyFontFamily);
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
                        //drawingContext.DrawGlyphRun();
                        var runProperty = charData.RunProperty;
                        var fontSize = runProperty.FontSize;

                        // todo 支持多个字符一起排版
                        var text = charData.CharObject.ToText();
                        List<ushort> glyphIndices = new List<ushort>();
                        for (var i = 0; i < text.Length; i++)
                        {
                            var c = text[i];
                            var glyphIndex = glyphTypeface.CharacterToGlyphMap[c];
                            glyphIndices.Add(glyphIndex);
                        }

                        List<double> advanceWidths = new List<double>();
                        var height = 0d;

                        for (var i = 0; i < text.Length; i++)
                        {
                            var glyphIndex = glyphIndices[i];

                            var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                            width = GlyphExtension.RefineValue(width);
                            advanceWidths.Add(width);

                            height = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;
                        }

                        var location = new System.Windows.Point(startPoint.X, startPoint.Y + height);
                        XmlLanguage defaultXmlLanguage =
                            XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);

                        var glyphRun = new GlyphRun
                        (
                            glyphTypeface,
                            bidiLevel: 0,
                            isSideways: false,
                            renderingEmSize: fontSize,
                            pixelsPerDip: pixelsPerDip,   // 只有在高版本的 .NET 才有此参数
                            glyphIndices: glyphIndices,
                            baselineOrigin: location,     // 设置文本的偏移量
                            advanceWidths: advanceWidths, // 设置每个字符的字宽，也就是字号
                            glyphOffsets: null,           // 设置每个字符的偏移量，可以为空
                            characters: text.ToCharArray(),
                            deviceFontName: null,
                            clusterMap: null,
                            caretStops: null,
                            language: defaultXmlLanguage
                        );

                        // todo 属性系统，支持设置前景色
                        Brush brush = Brushes.Black;
                        drawingContext.DrawGlyphRun(brush, glyphRun);

                        // todo 考虑加上缓存
                        lineRenderInfo.SetDrawnResult(new LineDrawnResult(null));
                    }
                }
            }
        }

        InvalidateVisual();
    }
}


internal class TextEditorPlatformProvider : PlatformProvider
{
    public TextEditorPlatformProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;

        _textLayoutDispatcherRequiring = new DispatcherRequiring(UpdateLayout, DispatcherPriority.Render);
        _charInfoMeasurer = new CharInfoMeasurer(textEditor);
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

    private readonly CharInfoMeasurer? _charInfoMeasurer;

    public override IRenderManager? GetRenderManager()
    {
        return TextEditor;
    }
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
        var runPropertyFontFamily = charInfo.RunProperty.FontFamily;
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

/// <summary>
/// 表示一种调度模型。在这种模型中，任务只能通过申请来执行，一系列申请结束后，任务会按照预定的优先级调度执行。
/// 这种模型适用于在频繁触发的事件中执行一个无需每次执行的逻辑。（类似于 InvalidateArrange 等。）
/// </summary>
internal class DispatcherRequiring : DispatcherObject
{
    private bool _isTaskRequired;
    private readonly Action _action;
    private readonly DispatcherPriority _priority = DispatcherPriority.Normal;

    /// <summary>
    /// 创建按照 <see cref="DispatcherPriority.Normal"/> 优先级调度执行 <paramref name="action"/> 的 <see cref="DispatcherRequiring"/> 的新实例。
    /// </summary>
    /// <param name="action">要进行的调度。</param>
    public DispatcherRequiring(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// 创建按照 <paramref name="priority"/> 优先级调度执行 <paramref name="action"/> 的 <see cref="DispatcherRequiring"/> 的新实例。
    /// </summary>
    /// <param name="action">要进行的调度。</param>
    /// <param name="priority">调度采用的优先级。</param>
    public DispatcherRequiring(Action action, DispatcherPriority priority)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _priority = priority;
    }

    /// <summary>
    /// 请求执行任务，以便在调度发生时开始执行。
    /// </summary>
    public void Require()
    {
        if (_isTaskRequired)
        {
            return;
        }
        _isTaskRequired = true;

        Dispatcher.InvokeAsync(InvokeAction, _priority);
    }

    /// <summary>
    /// 立即执行任务，执行完后，将清空所有之前对执行任务的请求。<para/>
    /// 默认情况下，如果此前没有申请执行过（没有调用 <see cref="Require"/> 方法），调用此方法将不会执行任务。
    /// 要更改此默认行为，请指定参数 <paramref name="withRequire"/> 决定是否立即申请一次，以便确保一定执行。
    /// </summary>
    /// <param name="withRequire">是否立即开始一次申请，如果开始，则无论此前是否申请过，都会开始执行任务。</param>
    public void Invoke(bool withRequire = false)
    {
        _isTaskRequired = _isTaskRequired || withRequire;
        InvokeAction();
    }

    /// <summary>
    /// 取消任务的执行。这样，即便调度开始，也不会执行指定的任务。
    /// </summary>
    public void Cancel()
    {
        _isTaskRequired = false;
    }

    private void InvokeAction()
    {
        if (!_isTaskRequired)
        {
            return;
        }
        try
        {
            _action.Invoke();
        }
        finally
        {
            _isTaskRequired = false;
        }
    }
}