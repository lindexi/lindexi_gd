using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Rendering;

using Math = System.Math;
using Rect = System.Windows.Rect;

namespace LightTextEditorPlus.Layout;

/// <summary>
/// 视觉呈现容器
/// </summary>
class TextView : UIElement, IRenderManager
{
    static TextView()
    {
        // 因为此类型永远不可被命中，所以直接重写并不再处理基类的命中测试改变方法。
        IsHitTestVisibleProperty.OverrideMetadata(typeof(TextView), new UIPropertyMetadata(false));
    }

    public TextView(TextEditor textEditor)
    {
        _textEditor = textEditor;

        _selectionAndCaretLayer = new SelectionAndCaretLayer(textEditor);

        _visualCollection = new VisualCollection(this)
        {
            _selectionAndCaretLayer
        };
    }

    private readonly TextEditor _textEditor;
    private readonly VisualCollection _visualCollection;

    private TextRenderBase? _textRenderBase;

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var textRender = GetTextRenderBase();

        var drawingVisual = textRender.Render(renderInfoProvider, _textEditor);

        // 需要加入逻辑树，且需要将旧的从逻辑树移除。否则将看不到文本
        if (_textDrawingVisual is not null)
        {
            _visualCollection.Remove(_textDrawingVisual);
        }

        _textDrawingVisual = drawingVisual;
        _visualCollection.Insert(0, drawingVisual);

        _selectionAndCaretLayer.UpdateSelectionAndCaret(renderInfoProvider);
        //InvalidateVisual();
    }

    private TextRenderBase GetTextRenderBase()
    {
        switch (_textEditor.TextEditorCore.ArrangingType)
        {
            case ArrangingType.Horizontal:
                if (_textRenderBase is HorizontalTextRender)
                {
                    return _textRenderBase;
                }
                else
                {
                    _textRenderBase = new HorizontalTextRender();
                    return _textRenderBase;
                }
            case ArrangingType.Vertical:
            case ArrangingType.Mongolian:
                // 还没有支持竖排渲染
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private DrawingVisual? _textDrawingVisual;
    private readonly SelectionAndCaretLayer _selectionAndCaretLayer;

    protected override Visual GetVisualChild(int index) => _visualCollection[index];

    protected override int VisualChildrenCount => _visualCollection.Count;

    #region 禁用命中测试

    // 只是用来呈现，不进行交互，关闭命中测试可以提升很多性能

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) => null;
    protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters) => null;

    #endregion
}

/// <summary>
/// 选择和光标的一层
/// </summary>
class SelectionAndCaretLayer : DrawingVisual, ICaretManager, ILayer
{
    public SelectionAndCaretLayer(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditor _textEditor;

    /// <summary>
    /// 更新光标和选择
    /// </summary>
    public void UpdateSelectionAndCaret(RenderInfoProvider renderInfoProvider)
    {
        if (!_textEditor.IsInEditingInputMode)
        {
            return;
        }

        _renderInfoProvider = renderInfoProvider;

        StartBlink();
    }

    private RenderInfoProvider? _renderInfoProvider;

    #region Caret

    /// <summary>
    /// 光标闪烁的时间
    /// </summary>
    private TimeSpan CaretBlinkTime
    {
        get
        {
            var caretBlinkTime = Win32Interop.GetCaretBlinkTime();
            // 要求闪烁至少是16毫秒。因为可能拿到 0 的值
            caretBlinkTime = Math.Max(16, caretBlinkTime);
            return TimeSpan.FromMilliseconds(caretBlinkTime);
        }
    }

    /// <summary>
    /// 用来控制光标的 <see cref="DispatcherTimer"/> 类型
    /// </summary>
    private CaretBlinkDispatcherTimer? _caretBlinkTimer;

    /// <summary>
    /// 开始闪烁光标
    /// </summary>
    private void StartBlink()
    {
        _textEditor.Logger.LogDebug("StartBlink 开始闪烁光标");

        // 一旦调用 开始闪烁光标 就需要第一次显示光标
        _isBlinkShown = false;

        _caretBlinkTimer?.Stop();
        _caretBlinkTimer ??= new CaretBlinkDispatcherTimer(this);
        _caretBlinkTimer.Interval = CaretBlinkTime;
        _caretBlinkTimer.Start();
    }

    /// <summary>
    /// 停止闪烁光标
    /// </summary>
    private void StopBlink()
    {
        _textEditor.Logger.LogDebug("StopBlink 停止闪烁光标");
        Debug.Assert(_caretBlinkTimer != null);
        _caretBlinkTimer?.Stop();

        HideCaret();
    }

    void ICaretManager.OnTick()
    {
        if (!_textEditor.IsInEditingInputMode)
        {
            // 如果没有进入编辑模式，按照文本库的规定，那就是不需要显示光标
            StopBlink();
            return;
        }

        if (_textEditor.TextEditorCore.IsDirty || _renderInfoProvider is null)
        {
            // 如果布局还没完成，那就啥也不用干，直接隐藏光标即可
            if (_isBlinkShown)
            {
                HideBlink();
               _isBlinkShown = false;
            }

            return;
        }

        var currentSelection = _textEditor.CurrentSelection;
        if (currentSelection.IsEmpty)
        {
            // 没有选择的情况，绘制和闪烁光标
            if (_isBlinkShown)
            {
                HideBlink();
            }
            else
            {
                // 由于判断了 _textEditor.TextEditorCore.IsDirty 因此不需要再等待布局完成
                //await _textEditor.TextEditorCore.WaitLayoutCompletedAsync();

                // 获取光标的坐标
                var caretRenderInfo = _renderInfoProvider.GetCaretRenderInfo(currentSelection.FrontOffset);
                var charData = caretRenderInfo.CharData;
                
                switch (_textEditor.TextEditorCore.ArrangingType)
                {
                    case ArrangingType.Horizontal:
                        var (x, y) = charData.GetStartPoint();
                        // 可以获取到起始点，那肯定存在尺寸
                        x += charData.Size!.Value.Width;
                        var width = 2;
                        var height = charData.Size!.Value.Height;
                        var foreground = charData.RunProperty.AsRunProperty().Foreground.Value;

                        var rectangle = new Rect(x,y,width,height);
                        var drawingContext = RenderOpen();
                        using (drawingContext)
                        {
                            drawingContext.DrawRectangle(foreground,null,rectangle);
                        }

                        break;
                    case ArrangingType.Vertical:
                        break;
                    case ArrangingType.Mongolian:
                        // todo 实现竖排的光标显示
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _isBlinkShown = !_isBlinkShown;
        }
        else
        {
            // todo 绘制选择内容
        }
    }

    /// <summary>
    /// 闪烁光标-隐藏
    /// </summary>
    private void HideBlink()
    {
        // 当前光标已经显示，那就是需要隐藏光标即可。啥都不显示
        var drawingContext = RenderOpen();
        using (drawingContext)
        {
            // 啥都不需要做，这就是清空
        }
    }

    /// <summary>
    /// 光标闪烁显示
    /// </summary>
    private bool _isBlinkShown;

    /// <summary>
    /// 隐藏光标
    /// </summary>
    private void HideCaret()
    {
        HideBlink();
    }

    #endregion
}