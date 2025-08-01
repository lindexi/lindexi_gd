using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LightTextEditorPlus.TextEditorPlus.Render;

namespace LightTextEditorPlus.TextEditorPlus.Layout
{
    /// <summary>
    /// 视觉呈现容器
    /// </summary>
    internal class TextView : FrameworkElement
    {
        public TextView(RenderManager renderManager)
        {
            _renderManager = renderManager;
            _layers = new UIElementCollection(this, this);

            HorizontalTextLayer = new HorizontalTextLayer();
            _layers.Add(HorizontalTextLayer);
        }

        public HorizontalTextLayer HorizontalTextLayer { get; }

        #region 属性

        /// <inheritdoc />
        protected override int VisualChildrenCount => _layers.Count;

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index) => _layers[index];

        protected override Size MeasureOverride(Size availableSize)
        {
            HorizontalTextLayer.Arrange(new Rect(new Point(), availableSize));

            return base.MeasureOverride(availableSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            //drawingContext.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(2, 2, 100, 100));
        }

        #endregion

        private readonly RenderManager _renderManager;
        private readonly UIElementCollection _layers;
    }

    class HorizontalTextLayer : Layer
    {
        public HorizontalTextLayer()
        {
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.Black, null, new Rect(10, 10, 100, 100));
            }

            //Visuals.Add(drawingVisual);
        }

        private List<Visual> Visuals { get; } = new();

        protected override void OnRender(DrawingContext drawingContext)
        {
            //drawingContext.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 1), new Rect(2, 2, 100, 100));

            // 这是没有被调用的
            base.OnRender(drawingContext);
        }

        /// <summary>
        ///   获取子元素的数目 <see cref="T:System.Windows.Media.Visual" />。
        /// </summary>
        /// <returns>子元素的数量。</returns>
        protected override int VisualChildrenCount => Visuals.Count;

        /// <summary>
        ///   返回指定 <see cref="T:System.Windows.Media.Visual" /> 的父代中 <see cref="T:System.Windows.Media.VisualCollection" />。
        /// </summary>
        /// <param name="index">
        ///   中的 visual 对象的索引 <see cref="T:System.Windows.Media.VisualCollection" />。
        /// </param>
        /// <returns>
        ///   中的子 <see cref="T:System.Windows.Media.VisualCollection" /> 指定 <paramref name="index" /> 值。
        /// </returns>
        protected override Visual GetVisualChild(int index) => Visuals[index];
    }

    /// <summary>
    /// 层的基类
    /// </summary>
    public class Layer : UIElement
    {
        static Layer()
        {
            // Focusable 由于父类已经是 false，所以无需设置。
            // FocusableProperty.OverrideMetadata(typeof(Layer), new UIPropertyMetadata(false));

            // 因为此类型永远不可被命中，所以直接重写并不再处理基类的命中测试改变方法。
            IsHitTestVisibleProperty.OverrideMetadata(typeof(Layer), new UIPropertyMetadata(false));
        }

        /// <summary>
        ///    实现 <see cref="M:System.Windows.Media.Visual.HitTestCore(System.Windows.Media.PointHitTestParameters)" /> 以提供基元素命中测试行为（返回 <see cref="T:System.Windows.Media.HitTestResult" />）。
        /// </summary>
        /// <param name="hitTestParameters">描述要执行的命中测试，包括初始命中点。</param>
        /// <returns>包括计算的点的测试结果。</returns>
        protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }

        /// <summary>
        ///   实现 <see cref="M:System.Windows.Media.Visual.HitTestCore(System.Windows.Media.GeometryHitTestParameters)" /> 以提供基元素命中测试行为 (返回 <see cref="T:System.Windows.Media.GeometryHitTestResult" />)。
        /// </summary>
        /// <param name="hitTestParameters">描述要执行的命中测试，包括初始命中点。</param>
        /// <returns>测试，包括计算的几何图形的结果。</returns>
        protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return null;
        }
    }
}
