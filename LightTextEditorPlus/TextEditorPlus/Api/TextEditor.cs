using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Document.DocumentManagers;

namespace LightTextEditorPlus.TextEditorPlus
{
    /// <summary>
    /// 文本编辑控件
    /// 支持复杂文本编辑，支持添加扩展字符包括图片等到文本
    /// </summary>
    /// <remarks> 这个项目的核心和入口就是这个类</remarks>
    public partial class TextEditor : FrameworkElement
    {
        public TextEditor()
        {
            DocumentManager = new DocumentManager(this);
        }

        public override void BeginInit()
        {
            base.BeginInit();
        }

        public override void EndInit()
        {
            // 在 XAML 设置，拿到所有 XAML 的属性
            // <textEditorPlus:TextEditor Text="123" /> 这里可以拿到 Text 属性的值
            base.EndInit();
        }

        public string Text { set; get; }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(100, 100);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        public DocumentManager DocumentManager { get; }

        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    base.OnRender(drawingContext);
        //}
    }
}
