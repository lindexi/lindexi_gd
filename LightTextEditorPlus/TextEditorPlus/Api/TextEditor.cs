using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace LightTextEditorPlus.TextEditorPlus
{
    /// <summary>
    /// 文本编辑控件
    /// 支持复杂文本编辑，支持添加扩展字符包括图片等到文本
    /// </summary>
    /// <remarks> 这个项目的核心和入口就是这个类</remarks>
    public partial class TextEditor : FrameworkElement
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    base.OnRender(drawingContext);
        //}
    }
}
