#if !USE_SKIA || USE_AllInOne

using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;

using System;

namespace LightTextEditorPlus;

// 此文件存放平台处理相关的逻辑
partial class TextEditor
{
#if USE_WPF || USE_AVALONIA // 其他平台暂时不知道是否能这么实现
    private TextSize MeasureTextEditorCore(TextSize availableSize)
    {
        // 此时可以通知文本底层进行布局了，这是一个很好的时机
        RenderInfoProvider renderInfoProvider = ForceLayout();

        DocumentLayoutBounds documentLayoutBounds = renderInfoProvider.GetDocumentLayoutBounds();
        TextRect documentContentBounds = documentLayoutBounds.DocumentContentBounds;

        (double x, double y, double width, double height) = documentLayoutBounds
            // 不应该取内容，应该取外接范围。解决垂直居中和底部对齐的问题
            .DocumentOutlineBounds;
        _ = x;
        _ = y;

        var notExistsWidth = double.IsInfinity(availableSize.Width) && double.IsNaN(Width);
        var notExistsHeight = double.IsInfinity(availableSize.Height) && double.IsNaN(Height);

        TextSizeToContent sizeToContent = TextEditorCore.SizeToContent;

        if (sizeToContent is TextSizeToContent.Width)
        {
            // 宽度自适应，高度固定
            if (notExistsWidth)
            {
                // 可能是放入无限宽度的容器中
                if (width < documentContentBounds.Width)
                {
                    // 内容宽度大于布局宽度，则使用内容宽度
                    width = documentContentBounds.Width;
                }

                if (double.IsInfinity(availableSize.Width))
                {
                    // 高度无穷的情况，应该得取小的范围，避免滚动条内，先拉大再缩小时，文本将获取拉大时的内容，无法再次缩小
                    width = documentContentBounds.Width;
                }
            }

            if (notExistsHeight)
            {
                throw new InvalidOperationException($"宽度自适应时，要求高度固定。{GetWidthAndHeightFormatMessage()}");
            }

            return new TextSize(width, availableSize.Height);
        }
        else if (sizeToContent is TextSizeToContent.Height)
        {
            // 高度自适应，宽度固定
            if (notExistsWidth)
            {
                throw new InvalidOperationException($"高度自适应，要求宽度固定。{GetWidthAndHeightFormatMessage()}");
            }

            if (notExistsHeight)
            {
                // 可能是放入无限高度的容器中
                if (height < documentContentBounds.Height)
                {
                    // 内容高度大于布局高度，则使用内容高度
                    height = documentContentBounds.Height;
                }

                if (double.IsInfinity(availableSize.Height))
                {
                    // 高度无穷的情况，应该得取小的范围，避免滚动条内，先拉大再缩小时，文本将获取拉大时的内容，无法再次缩小
                    height = documentContentBounds.Height;
                }
            }

            return new TextSize(availableSize.Width, height);
        }
        else if (sizeToContent is TextSizeToContent.WidthAndHeight)
        {
            // 宽度和高度都自适应
            if (notExistsWidth)
            {
                // 可能是放入无限宽度的容器中
                if (width < documentContentBounds.Width)
                {
                    // 内容宽度大于布局宽度，则使用内容宽度
                    width = documentContentBounds.Width;
                }

                if (double.IsInfinity(availableSize.Width))
                {
                    // 高度无穷的情况，应该得取小的范围，避免滚动条内，先拉大再缩小时，文本将获取拉大时的内容，无法再次缩小
                    width = documentContentBounds.Width;
                }
            }

            if (notExistsHeight)
            {
                // 可能是放入无限高度的容器中
                if (height < documentContentBounds.Height)
                {
                    // 内容高度大于布局高度，则使用内容高度
                    height = documentContentBounds.Height;
                }

                if (double.IsInfinity(availableSize.Height))
                {
                    // 高度无穷的情况，应该得取小的范围，避免滚动条内，先拉大再缩小时，文本将获取拉大时的内容，无法再次缩小
                    height = documentContentBounds.Height;
                }
            }

            return new TextSize(width, height);
        }
        else if (sizeToContent == TextSizeToContent.Manual)
        {
            if (notExistsWidth || notExistsHeight)
            {
                throw new InvalidOperationException($"设置为 SizeToContent 为 TextSizeToContent.Manual 手动时，不能无限定 {nameof(Width)} 和 {nameof(Height)} 放入无限尺寸的容器。{GetWidthAndHeightFormatMessage()}");
            }

            // 手动的，有多少就要多少
            return availableSize;
        }

        // 文本库，有多少就要多少
        return availableSize;

        string GetWidthAndHeightFormatMessage() =>
            $"AvailableSize={availableSize.Width:0.00},{availableSize.Height:0.00};Width={Width:0.00},Height={Height:0.00}";
    }
#endif
}
#endif
