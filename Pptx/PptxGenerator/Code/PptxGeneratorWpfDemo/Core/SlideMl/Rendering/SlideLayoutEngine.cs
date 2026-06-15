using System;
using System.Collections.Generic;
using System.Windows;

namespace PptxGenerator;

/// <summary>
/// 布局引擎实现，负责纯数学计算（坐标、尺寸、对齐），无 UI 框架依赖。
/// 仅使用 <see cref="Rect"/> 和 <see cref="Point"/> 等基础类型。
/// </summary>
internal sealed class SlideLayoutEngine : ISlideLayoutEngine
{
    /// <inheritdoc />
    public void PreLayout(SlidePage page, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        page.LayoutBounds = new Rect(0, 0, context.CanvasWidth, context.CanvasHeight);
        LayoutChildren(page.Children, page.LayoutBounds, parentId: "Page", clipToParent: false, context, useMeasured: false, measurements: null);
    }

    /// <inheritdoc />
    public void FinalLayout(SlidePage page, SlidePipelineContext context, SlideElementMeasurements measurements)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(measurements);

        page.LayoutBounds = new Rect(0, 0, context.CanvasWidth, context.CanvasHeight);
        LayoutChildren(page.Children, page.LayoutBounds, parentId: "Page", clipToParent: false, context, useMeasured: true, measurements);
    }

    private static void LayoutChildren(
        IReadOnlyList<SlideElement> children,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
    {
        foreach (var child in children)
        {
            LayoutElement(child, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
        }
    }

    private static void LayoutElement(
        SlideElement element,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
    {
        switch (element)
        {
            case SlidePanelElement panel:
                LayoutPanel(panel, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
                break;
            case SlideRectElement rect:
                LayoutRect(rect, parentBounds, parentId, clipToParent, context);
                break;
            case SlideTextElement text:
                LayoutText(text, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
                break;
            case SlideImageElement image:
                LayoutImage(image, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
                break;
            default:
                throw new InvalidOperationException($"未知元素类型: {element.GetType().Name}");
        }
    }

    private static void LayoutPanel(
        SlidePanelElement panel,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
    {
        if (panel.Layout == SlideLayoutDirection.Absolute)
        {
            LayoutAbsolutePanel(panel, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
        }
        else
        {
            LayoutFlowPanel(panel, parentBounds, parentId, clipToParent, context, useMeasured, measurements);
        }
    }

    private static void LayoutAbsolutePanel(
        SlidePanelElement panel,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
    {
        var provisionalWidth = panel.Width ?? Math.Max(0, parentBounds.Width - panel.Padding * 2);
        var provisionalHeight = panel.Height ?? Math.Max(0, parentBounds.Height - panel.Padding * 2);

        var initialChildOrigin = new Point(parentBounds.X + (panel.X ?? 0) + panel.Padding, parentBounds.Y + (panel.Y ?? 0) + panel.Padding);
        var provisionalContentBounds = new Rect(initialChildOrigin.X, initialChildOrigin.Y, provisionalWidth, provisionalHeight);

        LayoutChildren(panel.Children, provisionalContentBounds, panel.Id, clipToParent: true, context, useMeasured, measurements);

        var contentRight = 0d;
        var contentBottom = 0d;
        foreach (var child in panel.Children)
        {
            contentRight = Math.Max(contentRight, child.LocalBounds.Right);
            contentBottom = Math.Max(contentBottom, child.LocalBounds.Bottom);
        }

        var actualWidth = panel.Width ?? (contentRight + panel.Padding * 2);
        var actualHeight = panel.Height ?? (contentBottom + panel.Padding * 2);

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, actualWidth, panel.X, panel.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, actualHeight, panel.Y, panel.VerticalAlignment);

        panel.LocalBounds = new Rect(0, 0, actualWidth, actualHeight);
        panel.LayoutBounds = new Rect(originX, originY, actualWidth, actualHeight);
        panel.ActualWidth = actualWidth;
        panel.ActualHeight = actualHeight;

        var finalContentBounds = new Rect(originX + panel.Padding, originY + panel.Padding, Math.Max(0, actualWidth - panel.Padding * 2), Math.Max(0, actualHeight - panel.Padding * 2));
        LayoutChildren(panel.Children, finalContentBounds, panel.Id, clipToParent: true, context, useMeasured, measurements);

        ValidateBounds(panel, parentBounds, parentId, clipToParent, context);
    }

    private static void LayoutFlowPanel(
        SlidePanelElement panel,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
    {
        var isHorizontal = panel.Layout == SlideLayoutDirection.Horizontal;

        // 先确定 Panel 自身的内容区域起点
        var contentOriginX = parentBounds.X + (panel.X ?? 0) + panel.Padding;
        var contentOriginY = parentBounds.Y + (panel.Y ?? 0) + panel.Padding;

        // 第一步：用声明尺寸（或实测尺寸）计算每个子元素的尺寸
        var childSizes = new List<(SlideElement Child, double Width, double Height)>(panel.Children.Count);
        foreach (var child in panel.Children)
        {
            var (w, h) = GetChildSize(child, useMeasured, measurements);
            childSizes.Add((child, w, h));
        }

        // 内容区域的跨轴尺寸：优先使用 Panel 声明的尺寸，否则使用父容器尺寸
        var crossAxisContentSize = isHorizontal
            ? (panel.Height ?? Math.Max(0, parentBounds.Height - panel.Padding * 2))
            : (panel.Width ?? Math.Max(0, parentBounds.Width - panel.Padding * 2));

        // 第二步：沿流方向排列子元素
        var flowPosition = isHorizontal ? contentOriginX : contentOriginY;
        var crossAxisSize = 0d;

        for (var i = 0; i < childSizes.Count; i++)
        {
            var (child, childWidth, childHeight) = childSizes[i];
            var margin = child.Margin;

            // 流方向上的前导间距和尾随间距
            var leadingMargin = isHorizontal ? (margin?.Left ?? 0) : (margin?.Top ?? 0);
            var trailingMargin = isHorizontal ? (margin?.Right ?? 0) : (margin?.Bottom ?? 0);

            // 应用 Gap（第一个元素只加 leadingMargin，后续元素加 effectiveGap）
            if (i > 0)
            {
                var prevChild = childSizes[i - 1];
                var prevTrailingMargin = isHorizontal ? (prevChild.Child.Margin?.Right ?? 0) : (prevChild.Child.Margin?.Bottom ?? 0);
                // effectiveGap = max(panel.Gap, prev.trailingMargin + current.leadingMargin)
                // leadingMargin 已包含在 effectiveGap 中，不应再加
                var effectiveGap = Math.Max(panel.Gap, prevTrailingMargin + leadingMargin);
                flowPosition += effectiveGap;
            }
            else
            {
                // 第一个元素：只加 leadingMargin
                flowPosition += leadingMargin;
            }

            // 跨轴方向：使用子元素的显式偏移或对齐
            double crossOrigin;
            if (isHorizontal)
            {
                crossOrigin = ResolveOrigin(contentOriginY, crossAxisContentSize, childHeight, child.Y, child.VerticalAlignment);
            }
            else
            {
                crossOrigin = ResolveOrigin(contentOriginX, crossAxisContentSize, childWidth, child.X, child.HorizontalAlignment);
            }

            // 设置子元素的 LayoutBounds
            if (isHorizontal)
            {
                child.LocalBounds = new Rect(child.X ?? 0, child.Y ?? 0, childWidth, childHeight);
                child.LayoutBounds = new Rect(flowPosition, crossOrigin, childWidth, childHeight);
            }
            else
            {
                child.LocalBounds = new Rect(child.X ?? 0, child.Y ?? 0, childWidth, childHeight);
                child.LayoutBounds = new Rect(crossOrigin, flowPosition, childWidth, childHeight);
            }

            child.ActualWidth = childWidth;
            child.ActualHeight = childHeight;

            // 更新流位置（不含 trailingMargin，它只参与元素间 gap 计算）
            flowPosition += (isHorizontal ? childWidth : childHeight);

            // 更新跨轴最大尺寸
            var crossEnd = isHorizontal
                ? child.LayoutBounds.Y + childHeight
                : child.LayoutBounds.X + childWidth;
            crossAxisSize = Math.Max(crossAxisSize, crossEnd - (isHorizontal ? contentOriginY : contentOriginX));

            // 递归布局子 Panel
            if (child is SlidePanelElement childPanel)
            {
                LayoutPanel(childPanel, child.LayoutBounds, childPanel.Id, clipToParent: true, context, useMeasured, measurements);
            }

            ValidateBounds(child, parentBounds, panel.Id, clipToParent: true, context);
        }

        // 加上最后一个元素的 trailingMargin 计算总流尺寸
        if (childSizes.Count > 0)
        {
            var lastChild = childSizes[^1];
            var lastTrailingMargin = isHorizontal ? (lastChild.Child.Margin?.Right ?? 0) : (lastChild.Child.Margin?.Bottom ?? 0);
            flowPosition += lastTrailingMargin;
        }

        // 第三步：计算 Panel 自身尺寸
        var totalFlowSize = flowPosition - (isHorizontal ? contentOriginX : contentOriginY);
        var actualWidth = panel.Width ?? (isHorizontal ? totalFlowSize + panel.Padding * 2 : crossAxisSize + panel.Padding * 2);
        var actualHeight = panel.Height ?? (isHorizontal ? crossAxisSize + panel.Padding * 2 : totalFlowSize + panel.Padding * 2);

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, actualWidth, panel.X, panel.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, actualHeight, panel.Y, panel.VerticalAlignment);

        panel.LocalBounds = new Rect(0, 0, actualWidth, actualHeight);
        panel.LayoutBounds = new Rect(originX, originY, actualWidth, actualHeight);
        panel.ActualWidth = actualWidth;
        panel.ActualHeight = actualHeight;

        // 第四步：偏移所有子元素到 Panel 的最终位置
        var offsetX = originX + panel.Padding - contentOriginX;
        var offsetY = originY + panel.Padding - contentOriginY;
        foreach (var (child, _, _) in childSizes)
        {
            child.LayoutBounds = new Rect(
                child.LayoutBounds.X + offsetX,
                child.LayoutBounds.Y + offsetY,
                child.LayoutBounds.Width,
                child.LayoutBounds.Height);
        }

        // 溢出检测
        if (panel.Width is double fixedWidth && totalFlowSize > fixedWidth + 0.1)
        {
            context.AddWarning($"[Warning] {panel.Id}: 流式布局内容宽度 {SlideXmlUtilities.FormatNumber(totalFlowSize)} 超出 Panel 宽度 {SlideXmlUtilities.FormatNumber(fixedWidth)}");
        }

        if (panel.Height is double fixedHeight && totalFlowSize > fixedHeight + 0.1)
        {
            context.AddWarning($"[Warning] {panel.Id}: 流式布局内容高度 {SlideXmlUtilities.FormatNumber(totalFlowSize)} 超出 Panel 高度 {SlideXmlUtilities.FormatNumber(fixedHeight)}");
        }

        ValidateBounds(panel, parentBounds, parentId, clipToParent, context);
    }

    private static (double Width, double Height) GetChildSize(
        SlideElement child,
        bool useMeasured,
        SlideElementMeasurements? measurements)
    {
        if (useMeasured && measurements is not null && measurements.TryGetValue(child.Id, out var measureResult))
        {
            return (child.Width ?? measureResult.MeasuredWidth, child.Height ?? measureResult.MeasuredHeight);
        }

        return child switch
        {
            SlideTextElement => (child.Width ?? 0, child.Height ?? 0),
            SlideImageElement => (child.Width ?? 240, child.Height ?? 180),
            SlideRectElement => (child.Width ?? 0, child.Height ?? 0),
            _ => (child.Width ?? 0, child.Height ?? 0),
        };
    }

    private static void LayoutRect(
        SlideRectElement rect,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context)
    {
        var width = rect.Width ?? 0;
        var height = rect.Height ?? 0;
        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, width, rect.X, rect.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, height, rect.Y, rect.VerticalAlignment);

        rect.LocalBounds = new Rect(rect.X ?? 0, rect.Y ?? 0, width, height);
        rect.LayoutBounds = new Rect(originX, originY, width, height);
        rect.ActualWidth = width;
        rect.ActualHeight = height;

        ValidateBounds(rect, parentBounds, parentId, clipToParent, context);
    }

    private static void LayoutText(
        SlideTextElement text,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
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

        text.LocalBounds = new Rect(text.X ?? 0, text.Y ?? 0, measuredWidth, measuredHeight);
        text.LayoutBounds = new Rect(originX, originY, measuredWidth, measuredHeight);
        text.ActualWidth = measuredWidth;
        text.ActualHeight = measuredHeight;

        if (useMeasured && text.Height is double fixedHeight && measurements is not null && measurements.TryGetValue(text.Id, out var mr))
        {
            var lineHeight = text.FontSize * text.LineHeight;
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
        SlideImageElement image,
        Rect parentBounds,
        string parentId,
        bool clipToParent,
        SlidePipelineContext context,
        bool useMeasured,
        SlideElementMeasurements? measurements)
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

        image.LocalBounds = new Rect(image.X ?? 0, image.Y ?? 0, width, height);
        image.LayoutBounds = new Rect(originX, originY, width, height);
        image.ActualWidth = width;
        image.ActualHeight = height;

        ValidateBounds(image, parentBounds, parentId, clipToParent, context);
    }

    private static void ValidateBounds(SlideElement element, Rect parentBounds, string parentId, bool clipToParent, SlidePipelineContext context)
    {
        var bounds = element.LayoutBounds;
        if (bounds.Right > context.CanvasWidth)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素右边界 X={SlideXmlUtilities.FormatNumber(bounds.Right)} 超出画布宽度 {context.CanvasWidth}");
        }

        if (bounds.Bottom > context.CanvasHeight)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素下边界 Y={SlideXmlUtilities.FormatNumber(bounds.Bottom)} 超出画布高度 {context.CanvasHeight}");
        }

        if (bounds.X < 0)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素左边界 X={SlideXmlUtilities.FormatNumber(bounds.X)} 超出画布左侧 0");
        }

        if (bounds.Y < 0)
        {
            context.AddWarning($"[Warning] {element.Id}: 元素上边界 Y={SlideXmlUtilities.FormatNumber(bounds.Y)} 超出画布顶部 0");
        }

        if (clipToParent && !parentBounds.Contains(bounds))
        {
            context.AddWarning($"[Warning] {element.Id}: 元素超出父容器 {parentId}，超出部分将被裁剪");
        }
    }

    internal static double ResolveOrigin(double parentOrigin, double parentSize, double elementSize, double? explicitOffset, SlideHorizontalAlignment? alignment)
    {
        if (explicitOffset is double x)
        {
            return parentOrigin + x;
        }

        return alignment switch
        {
            SlideHorizontalAlignment.Center => parentOrigin + Math.Max(0, (parentSize - elementSize) / 2),
            SlideHorizontalAlignment.Right => parentOrigin + Math.Max(0, parentSize - elementSize),
            _ => parentOrigin,
        };
    }

    internal static double ResolveOrigin(double parentOrigin, double parentSize, double elementSize, double? explicitOffset, SlideVerticalAlignment? alignment)
    {
        if (explicitOffset is double y)
        {
            return parentOrigin + y;
        }

        return alignment switch
        {
            SlideVerticalAlignment.Center => parentOrigin + Math.Max(0, (parentSize - elementSize) / 2),
            SlideVerticalAlignment.Bottom => parentOrigin + Math.Max(0, parentSize - elementSize),
            _ => parentOrigin,
        };
    }
}
