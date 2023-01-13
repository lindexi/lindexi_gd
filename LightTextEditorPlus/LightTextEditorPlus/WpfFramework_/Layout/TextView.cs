using System;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Rendering;

namespace LightTextEditorPlus.Layout;

/// <summary>
/// 视觉呈现容器
/// </summary>
class TextView : UIElement, IRenderManager
{
    public TextView(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditor _textEditor;
    private TextRenderBase? _textRenderBase;

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var textRender = GetTextRenderBase();

        var drawingVisual = textRender.Render(renderInfoProvider,_textEditor);

        if (_drawingVisual is not null)
        {
            RemoveVisualChild(_drawingVisual);
        }

        _drawingVisual = drawingVisual;
        AddVisualChild(drawingVisual);

        InvalidateVisual();
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

    private DrawingVisual? _drawingVisual;

    protected override Visual GetVisualChild(int index) => _drawingVisual!;

    protected override int VisualChildrenCount => _drawingVisual is null ? 0 : 1;

    #region 禁用命中测试
    // 只是用来呈现，不进行交互，关闭命中测试可以提升很多性能

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) => null;
    protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters) => null;

    #endregion

}