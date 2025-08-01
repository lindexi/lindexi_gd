using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Document.DocumentManagers;
using LightTextEditorPlus.TextEditorPlus.Editing;
using LightTextEditorPlus.TextEditorPlus.Layout;
using LightTextEditorPlus.TextEditorPlus.Render;

namespace LightTextEditorPlus.TextEditorPlus
{
    /// <summary>
    /// 文本编辑控件
    /// 支持复杂文本编辑，支持添加扩展字符包括图片等到文本
    /// </summary>
    /// <remarks> 这个项目的核心和入口就是这个类</remarks>
    public partial class TextEditor : FrameworkElement, IIMETextEditor
    {
        public TextEditor()
        {
            DocumentManager = new DocumentManager(this);
            RenderManager = new RenderManager(this);

            _textView = new TextView(RenderManager);
            // 需要将子元素加入到可视化树以便在子元素发生改变之后能够自行重绘。
            // 如果你决定完全自己接手重绘逻辑（就像 DrawingVisual.RenderOpen 那样），那么你可以不将其加入到可视化树中。
            AddVisualChild(_textView); // 让 _textView 可以找到 Parent 从而可以交互

            _imeSupporter = new IMESupporter<TextEditor>(this);

        }

        static TextEditor()
        {
            // 用于接收 Tab 按键，而不是被切换焦点
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TextEditor),
                new FrameworkPropertyMetadata(true));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TextEditor),
                new FrameworkPropertyMetadata(KeyboardNavigationMode.None));

            // 用于获取焦点逻辑
            FocusableProperty.OverrideMetadata(typeof(TextEditor),
                new FrameworkPropertyMetadata(true));
        }

        protected override Visual GetVisualChild(int index) => _textView; // 让外层可以找到里层，从而里层可以被渲染
        protected override int VisualChildrenCount => 1;

        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    drawingContext.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(2, 2, 100, 100));
        //    base.OnRender(drawingContext);
        //}

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

        public string Text { set; get; } = string.Empty;

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

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Black,null,new Rect(MouseDownPoint,new Size(3,30)));
            base.OnRender(drawingContext);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        #region IME

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            MouseDownPoint = e.GetPosition(this);
            Focus();
            InvalidateVisual();
        }
        
        private Point MouseDownPoint { get; set; }

        string IIMETextEditor.GetFontFamilyName()
        {
            return "微软雅黑";
        }

        int IIMETextEditor.GetFontSize()
        {
            return 30;
        }

        Point IIMETextEditor.GetTextEditorLeftTop()
        {
            // 相对于当前输入框的坐标
            return new Point(0, 0);
        }

        Point IIMETextEditor.GetCaretLeftTop()
        {
            return MouseDownPoint;
        }

        private readonly IMESupporter<TextEditor> _imeSupporter;

        #endregion
    }
}
