using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Rendering;

/// <summary>
/// 布局引擎实现，负责纯数学计算（坐标、尺寸、对齐），无 UI 框架依赖。
/// 仅使用 <see cref="SlideMlRect"/> 和 <see cref="SlideMlPoint"/> 等纯数据类型。
/// </summary>
public sealed class SlideMlLayoutEngine : ISlideMlLayoutEngine
{
    /// <inheritdoc />
    public void PreLayout(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        page.LayoutBounds = new SlideMlRect(0, 0, context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight);
        LayoutChildren(page.Children, page.LayoutBounds, parentId: "Page", clipToParent: false, context, useMeasured: false, measurements: null);
    }

    /// <inheritdoc />
    public void FinalLayout(SlideMlPage page, SlideMlPipelineContext context, SlideMlElementMeasurements measurements)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(measurements);

        page.LayoutBounds = new SlideMlRect(0, 0, context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight);
        LayoutChildren(page.Children, page.LayoutBounds, parentId: "Page", clipToParent: false, context, useMeasured: true, measurements);
            }

    private static void LayoutChildren(
        IReadOnlyList<SlideMlElement> children,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        foreach (var child in children)
        {
            LayoutElement(child, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
        }
    }

    private static void LayoutElement(
        SlideMlElement element,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        switch (element)
        {
            case SlideMlPanelElement panel:
                LayoutPanel(panel, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
                break;
            case SlideMlRectElement rect:
                LayoutRect(rect, parentBounds, parentId, clipToParent, context);
                break;
            case SlideMlTextElement text:
                LayoutText(text, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
                break;
            case SlideMlImageElement image:
                LayoutImage(image, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
                break;
            default:
                throw new SlideMlUnsupportedElementException($"未知元素类型: {element.GetType().Name}", element.GetType().Name);
        }
    }

    private static void LayoutPanel(
        SlideMlPanelElement panel,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        if (panel.Layout == SlideMlLayoutDirection.Absolute)
        {
            LayoutAbsolutePanel(panel, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
        }
        else
        {
            LayoutFlowPanel(panel, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
        }
    }

    private static void LayoutAbsolutePanel(
        SlideMlPanelElement panel,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        var paddingLeft = panel.Padding.Left;
        var paddingTop = panel.Padding.Top;
        var paddingRight = panel.Padding.Right;
        var paddingBottom = panel.Padding.Bottom;

        var provisionalWidth = panel.Width ?? Math.Max(0, parentBounds.Width - paddingLeft - paddingRight);
        var provisionalHeight = panel.Height ?? Math.Max(0, parentBounds.Height - paddingTop - paddingBottom);

        var initialChildOriginX = parentBounds.X + (panel.X ?? 0) + paddingLeft;
        var initialChildOriginY = parentBounds.Y + (panel.Y ?? 0) + paddingTop;
        var provisionalContentBounds = new SlideMlRect(initialChildOriginX, initialChildOriginY, provisionalWidth, provisionalHeight);

        LayoutChildren(panel.Children, provisionalContentBounds, panel.Id, clipToParent: true, context, useMeasured, measurements);

        var contentRight = 0d;
        var contentBottom = 0d;
        foreach (var child in panel.Children)
        {
            contentRight = Math.Max(contentRight, child.LocalBounds.Right);
            contentBottom = Math.Max(contentBottom, child.LocalBounds.Bottom);
        }

        var actualWidth = panel.Width ?? (contentRight + paddingLeft + paddingRight);
        var actualHeight = panel.Height ?? (contentBottom + paddingTop + paddingBottom);

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, actualWidth, panel.X, panel.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, actualHeight, panel.Y, panel.VerticalAlignment);

        panel.LocalBounds = new SlideMlRect(0, 0, actualWidth, actualHeight);
        panel.LayoutBounds = new SlideMlRect(originX, originY, actualWidth, actualHeight);
        panel.MeasuredWidth = actualWidth;
        panel.MeasuredHeight = actualHeight;

        var finalContentBounds = new SlideMlRect(originX + paddingLeft, originY + paddingTop, Math.Max(0, actualWidth - paddingLeft - paddingRight), Math.Max(0, actualHeight - paddingTop - paddingBottom));
        LayoutChildren(panel.Children, finalContentBounds, panel.Id, clipToParent: true, context, useMeasured, measurements);

        ValidateBounds(panel, parentBounds, parentId, clipToParent, context);
    }

    private static void LayoutFlowPanel(
        SlideMlPanelElement panel,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        var isHorizontal = panel.Layout == SlideMlLayoutDirection.Horizontal;
        var paddingLeft = panel.Padding.Left;
        var paddingTop = panel.Padding.Top;
        var paddingRight = panel.Padding.Right;
        var paddingBottom = panel.Padding.Bottom;

        var contentOriginX = parentBounds.X + (panel.X ?? 0) + paddingLeft;
        var contentOriginY = parentBounds.Y + (panel.Y ?? 0) + paddingTop;

        var childSizes = new List<(SlideMlElement Child, double Width, double Height)>(panel.Children.Count);
        foreach (var child in panel.Children)
        {
            var (w, h) = GetChildSize(child, useMeasured, measurements);
            childSizes.Add((child, w, h));
        }

        var crossAxisContentSize = isHorizontal
            ? (panel.Height ?? Math.Max(0, parentBounds.Height - paddingTop - paddingBottom))
            : (panel.Width ?? Math.Max(0, parentBounds.Width - paddingLeft - paddingRight));

        var flowPosition = isHorizontal ? contentOriginX : contentOriginY;
        var crossAxisSize = 0d;
        double? lastInFlowTrailingMargin = null;

        for (var i = 0; i < childSizes.Count; i++)
        {
            var (child, childWidth, childHeight) = childSizes[i];

            // 判断子元素在主轴上是否有显式坐标：有则脱离流式布局，使用绝对定位
            var hasMainAxisExplicit = isHorizontal ? child.X.HasValue : child.Y.HasValue;

            if (hasMainAxisExplicit)
            {
                // 脱离流：使用显式坐标定位，不推进流式游标，不参与跨轴尺寸计算
                var mainAxisPosition = isHorizontal
                    ? contentOriginX + child.X!.Value
                    : contentOriginY + child.Y!.Value;

                double absCrossOrigin;
                if (isHorizontal)
                {
                    absCrossOrigin = ResolveOrigin(contentOriginY, crossAxisContentSize, childHeight, child.Y, child.VerticalAlignment);
                }
                else
                {
                    absCrossOrigin = ResolveOrigin(contentOriginX, crossAxisContentSize, childWidth, child.X, child.HorizontalAlignment);
                }

                if (isHorizontal)
                {
                    child.LocalBounds = new SlideMlRect(child.X ?? 0, child.Y ?? 0, childWidth, childHeight);
                    child.LayoutBounds = new SlideMlRect(mainAxisPosition, absCrossOrigin, childWidth, childHeight);
                }
                else
                {
                    child.LocalBounds = new SlideMlRect(child.X ?? 0, child.Y ?? 0, childWidth, childHeight);
                    child.LayoutBounds = new SlideMlRect(absCrossOrigin, mainAxisPosition, childWidth, childHeight);
                }

                child.MeasuredWidth = childWidth;
                child.MeasuredHeight = childHeight;

                if (child is SlideMlPanelElement absChildPanel)
                {
                    LayoutPanel(absChildPanel, child.LayoutBounds, absChildPanel.Id, clipToParent: true, context, useMeasured, measurements);
                }

                ValidateBounds(child, parentBounds, panel.Id, clipToParent: true, context);
                continue;
            }

            // 流式布局：沿主轴依次排列
            var margin = child.Margin;
            var leadingMargin = isHorizontal ? (margin?.Left ?? 0) : (margin?.Top ?? 0);
            var trailingMargin = isHorizontal ? (margin?.Right ?? 0) : (margin?.Bottom ?? 0);

            if (lastInFlowTrailingMargin is double prevTrailingMargin)
            {
                var effectiveGap = Math.Max(panel.Gap, prevTrailingMargin + leadingMargin);
                flowPosition += effectiveGap;
            }
            else
            {
                flowPosition += leadingMargin;
            }

            double crossOrigin;
            if (isHorizontal)
            {
                crossOrigin = ResolveOrigin(contentOriginY, crossAxisContentSize, childHeight, child.Y, child.VerticalAlignment);
            }
            else
            {
                crossOrigin = ResolveOrigin(contentOriginX, crossAxisContentSize, childWidth, child.X, child.HorizontalAlignment);
            }

            if (isHorizontal)
            {
                child.LocalBounds = new SlideMlRect(child.X ?? 0, child.Y ?? 0, childWidth, childHeight);
                child.LayoutBounds = new SlideMlRect(flowPosition, crossOrigin, childWidth, childHeight);
            }
            else
            {
                child.LocalBounds = new SlideMlRect(child.X ?? 0, child.Y ?? 0, childWidth, childHeight);
                child.LayoutBounds = new SlideMlRect(crossOrigin, flowPosition, childWidth, childHeight);
            }

            child.MeasuredWidth = childWidth;
            child.MeasuredHeight = childHeight;

            flowPosition += (isHorizontal ? childWidth : childHeight);

            var crossEnd = isHorizontal
                ? child.LayoutBounds.Y + childHeight
                : child.LayoutBounds.X + childWidth;
            crossAxisSize = Math.Max(crossAxisSize, crossEnd - (isHorizontal ? contentOriginY : contentOriginX));

            if (child is SlideMlPanelElement childPanel)
            {
                LayoutPanel(childPanel, child.LayoutBounds, childPanel.Id, clipToParent: true, context, useMeasured, measurements);
            }

            ValidateBounds(child, parentBounds, panel.Id, clipToParent: true, context);

            lastInFlowTrailingMargin = trailingMargin;
        }

        if (lastInFlowTrailingMargin is double trailing)
        {
            flowPosition += trailing;
        }

        var totalFlowSize = flowPosition - (isHorizontal ? contentOriginX : contentOriginY);
        var actualWidth = panel.Width ?? (isHorizontal ? totalFlowSize + paddingLeft + paddingRight : crossAxisSize + paddingLeft + paddingRight);
        var actualHeight = panel.Height ?? (isHorizontal ? crossAxisSize + paddingTop + paddingBottom : totalFlowSize + paddingTop + paddingBottom);

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, actualWidth, panel.X, panel.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, actualHeight, panel.Y, panel.VerticalAlignment);

        panel.LocalBounds = new SlideMlRect(0, 0, actualWidth, actualHeight);
        panel.LayoutBounds = new SlideMlRect(originX, originY, actualWidth, actualHeight);
        panel.MeasuredWidth = actualWidth;
        panel.MeasuredHeight = actualHeight;

        var offsetX = originX + paddingLeft - contentOriginX;
        var offsetY = originY + paddingTop - contentOriginY;
        foreach (var (child, _, _) in childSizes)
        {
            child.LayoutBounds = new SlideMlRect(
                child.LayoutBounds.X + offsetX,
                child.LayoutBounds.Y + offsetY,
                child.LayoutBounds.Width,
                child.LayoutBounds.Height);
        }

        if (panel.Width is double fixedWidth && totalFlowSize > fixedWidth + 0.1)
        {
            context.AddWarning($"[Warning] {panel.Id}: 流式布局内容宽度 {SlideMlXmlUtilities.FormatNumber(totalFlowSize)} 超出 Panel 宽度 {SlideMlXmlUtilities.FormatNumber(fixedWidth)}");
        }

        if (panel.Height is double fixedHeight && totalFlowSize > fixedHeight + 0.1)
        {
            context.AddWarning($"[Warning] {panel.Id}: 流式布局内容高度 {SlideMlXmlUtilities.FormatNumber(totalFlowSize)} 超出 Panel 高度 {SlideMlXmlUtilities.FormatNumber(fixedHeight)}");
        }

        ValidateBounds(panel, parentBounds, parentId, clipToParent, context);
    }

    private static (double Width, double Height) GetChildSize(
        SlideMlElement child,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        if (useMeasured && measurements is not null && measurements.TryGetValue(child.Id, out var measureResult))
        {
            return (child.Width ?? measureResult.MeasuredWidth, child.Height ?? measureResult.MeasuredHeight);
        }

        return child switch
        {
            SlideMlTextElement => (child.Width ?? 0, child.Height ?? 0),
            SlideMlImageElement => (child.Width ?? 240, child.Height ?? 180),
            SlideMlRectElement => (child.Width ?? 0, child.Height ?? 0),
            _ => (child.Width ?? 0, child.Height ?? 0),
        };
    }

    private static void LayoutRect(
        SlideMlRectElement rect,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context)
    {
        var width = rect.Width ?? 0;
        var height = rect.Height ?? 0;
        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, width, rect.X, rect.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, height, rect.Y, rect.VerticalAlignment);

        rect.LocalBounds = new SlideMlRect(rect.X ?? 0, rect.Y ?? 0, width, height);
        rect.LayoutBounds = new SlideMlRect(originX, originY, width, height);
        rect.MeasuredWidth = width;
        rect.MeasuredHeight = height;

        ValidateBounds(rect, parentBounds, parentId, clipToParent, context);
    }

    private static void LayoutText(
        SlideMlTextElement text,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        double measuredWidth;
        double measuredHeight;

        if (useMeasured && measurements is not null && measurements.TryGetValue(text.Id, out var measureResult))
        {
            measuredWidth = text.Width ?? measureResult.MeasuredWidth;
            measuredHeight = text.Height ?? measureResult.MeasuredHeight;
            text.ActualLineCount = measureResult.ActualLineCount ?? text.ActualLineCount;
        }
        else
        {
            measuredWidth = text.Width ?? 0;
            measuredHeight = text.Height ?? 0;
        }

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, measuredWidth, text.X, text.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, measuredHeight, text.Y, text.VerticalAlignment);

        text.LocalBounds = new SlideMlRect(text.X ?? 0, text.Y ?? 0, measuredWidth, measuredHeight);
        text.LayoutBounds = new SlideMlRect(originX, originY, measuredWidth, measuredHeight);
        text.MeasuredWidth = measuredWidth;
        text.MeasuredHeight = measuredHeight;

        if (useMeasured && text.Height is double fixedHeight && measurements is not null && measurements.TryGetValue(text.Id, out var mr))
        {
            var lineHeight = text.FontSize;
            if (mr.MeasuredHeight > fixedHeight + 0.1)
            {
                var averageLineHeight = text.ActualLineCount == 0 ? lineHeight : mr.MeasuredHeight / text.ActualLineCount;
                var visibleLineCount = averageLineHeight <= 0 ? 0 : Math.Max(0, (int)Math.Floor(fixedHeight / averageLineHeight));
                context.AddWarning($"[Warning] {text.Id}: ActualLineCount={text.ActualLineCount}，超出容器高度（当前高度仅容纳 {visibleLineCount} 行）");
            }
        }

        ValidateBounds(text, parentBounds, parentId, clipToParent, context);
    }

    private static void LayoutImage(
        SlideMlImageElement image,
        SlideMlRect parentBounds,
        string parentId,
        bool clipToParent,
        SlideMlPipelineContext context,
        bool useMeasured,
        SlideMlElementMeasurements? measurements)
    {
        double width;
        double height;

        if (useMeasured && measurements is not null && measurements.TryGetValue(image.Id, out var measureResult))
        {
            width = image.Width ?? measureResult.MeasuredWidth;
            height = image.Height ?? measureResult.MeasuredHeight;
        }
        else
        {
            width = image.Width ?? 240;
            height = image.Height ?? 180;
        }

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, width, image.X, image.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, height, image.Y, image.VerticalAlignment);

        image.LocalBounds = new SlideMlRect(image.X ?? 0, image.Y ?? 0, width, height);
        image.LayoutBounds = new SlideMlRect(originX, originY, width, height);
        image.MeasuredWidth = width;
        image.MeasuredHeight = height;

        ValidateBounds(image, parentBounds, parentId, clipToParent, context);
    }

    private static void ValidateBounds(SlideMlElement element, SlideMlRect parentBounds, string parentId, bool clipToParent, SlideMlPipelineContext context)
    {
        var bounds = element.LayoutBounds;
        if (bounds.Right > context.SlideDocumentContext.CanvasWidth)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素右边界 X={SlideMlXmlUtilities.FormatNumber(bounds.Right)} 超出画布宽度 {context.SlideDocumentContext.CanvasWidth}");
        }

        if (bounds.Bottom > context.SlideDocumentContext.CanvasHeight)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素下边界 Y={SlideMlXmlUtilities.FormatNumber(bounds.Bottom)} 超出画布高度 {context.SlideDocumentContext.CanvasHeight}");
        }

        if (bounds.X < 0)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素左边界 X={SlideMlXmlUtilities.FormatNumber(bounds.X)} 超出画布左侧 0");
        }

        if (bounds.Y < 0)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素上边界 Y={SlideMlXmlUtilities.FormatNumber(bounds.Y)} 超出画布顶部 0");
        }

        if (clipToParent && !SlideRectContains(parentBounds, bounds))
        {
            context.AddWarning($"[Warning] {element.Id}: 元素超出父容器 {parentId}，超出部分将被裁剪");
        }
    }

    /// <summary>
    /// 判断 <paramref name="container"/> 是否完全包含 <paramref name="inner"/>。
    /// </summary>
    internal static bool SlideRectContains(SlideMlRect container, SlideMlRect inner)
    {
        return inner.X >= container.X
            && inner.Y >= container.Y
            && inner.Right <= container.Right
            && inner.Bottom <= container.Bottom;
    }

    /// <summary>
    /// 解析元素原点坐标（水平方向）。
    /// </summary>
    internal static double ResolveOrigin(double parentOrigin, double parentSize, double elementSize, double? explicitOffset, SlideMlHorizontalAlignment? alignment)
    {
        if (explicitOffset is double x)
        {
            return parentOrigin + x;
        }

        return alignment switch
        {
            SlideMlHorizontalAlignment.Center => parentOrigin + Math.Max(0, (parentSize - elementSize) / 2),
            SlideMlHorizontalAlignment.Right => parentOrigin + Math.Max(0, parentSize - elementSize),
            _ => parentOrigin,
        };
    }

    /// <summary>
    /// 解析元素原点坐标（垂直方向）。
    /// </summary>
    internal static double ResolveOrigin(double parentOrigin, double parentSize, double elementSize, double? explicitOffset, SlideMlVerticalAlignment? alignment)
    {
        if (explicitOffset is double y)
        {
            return parentOrigin + y;
        }

        return alignment switch
        {
            SlideMlVerticalAlignment.Center => parentOrigin + Math.Max(0, (parentSize - elementSize) / 2),
            SlideMlVerticalAlignment.Bottom => parentOrigin + Math.Max(0, parentSize - elementSize),
            _ => parentOrigin,
        };
    }
}
