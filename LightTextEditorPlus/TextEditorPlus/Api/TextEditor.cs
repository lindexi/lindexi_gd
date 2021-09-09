using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Document.DocumentManagers;
using LightTextEditorPlus.TextEditorPlus.Layout;
using LightTextEditorPlus.TextEditorPlus.Render;

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
            RenderManager = new RenderManager(this);

            _textView = new TextView(RenderManager);
            // 需要将子元素加入到可视化树以便在子元素发生改变之后能够自行重绘。
            // 如果你决定完全自己接手重绘逻辑（就像 DrawingVisual.RenderOpen 那样），那么你可以不将其加入到可视化树中。
            AddVisualChild(_textView);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(2, 2, 100, 100));
            base.OnRender(drawingContext);
        }

        public override void BeginInit()
        {
            base.BeginInit();
        }

        public override void EndInit()
        {
            // 在 XAML 设置，拿到所有 XAML 的属性
            // <textEditorPlus:TextEditor Text="123" /> 这里可以拿到 Text 属性的值
            DocumentManager.DocumentWidth = Width;
            DocumentManager.DocumentHeight = Height;

            base.EndInit();
        }

        public string Text { set; get; }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(100, 100);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _textView.Arrange(new Rect(new Point(), finalSize));
            return base.ArrangeOverride(finalSize);
        }

        public DocumentManager DocumentManager { get; }

        private RenderManager RenderManager { get; }

        /// <summary>
        /// 负责文本的渲染（不包含任何交互）。
        /// </summary>
        private readonly TextView _textView;

        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    base.OnRender(drawingContext);
        //}
    }
}
