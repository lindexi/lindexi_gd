using SkiaSharp;

namespace LightTextEditorPlus.Diagnostics
{
    /// <summary>
    /// 在 Skia 平台下的文本编辑器调试配置
    /// </summary>
    public class SkiaTextEditorDebugConfiguration
    {
        internal SkiaTextEditorDebugConfiguration(SkiaTextEditor textEditor)
        {
            TextEditor = textEditor;
        }

        public bool IsInDebugMode => TextEditor.TextEditorCore.IsInDebugMode;

        private SkiaTextEditor TextEditor { get; }

        /// <summary>
        /// 调试绘制每个字符边框的颜色
        /// </summary>
        /// <remarks>设置为 null 则代表不显示</remarks>
        public SKColor? DebugDrawCharBoundsColor { get; set; }

        /// <summary>
        /// 调试绘制每个连续字符 Span 的边界的颜色
        /// </summary>
        /// <remarks>设置为 null 则代表不显示</remarks>
        public SKColor? DebugDrawCharSpanBoundsColor { get; set; }

        /// <summary>
        /// 调试绘制每行的边界的颜色
        /// </summary>
        /// <remarks>设置为 null 则代表不显示</remarks>
        public SKColor? DebugDrawLineBoundsColor { get; set; }

        /// <summary>
        /// 在进入调试模式下，显示所有的调试边框。如 <see cref="IsInDebugMode"/> 不为 true 则不会生效
        /// </summary>
        public void ShowAllDebugBoundsWhenInDebugMode()
        {
            if (!IsInDebugMode)
            {
                return;
            }

            DebugDrawCharBoundsColor = SKColors.CadetBlue.WithAlpha(0xA0);
            DebugDrawCharSpanBoundsColor = SKColors.Red.WithAlpha(0xA0);
            DebugDrawLineBoundsColor = SKColors.Blue.WithAlpha(0x50);
        }

        /// <summary>
        /// 清理所有的调试边框
        /// </summary>
        public void ClearAllDebugBounds()
        {
            DebugDrawCharBoundsColor = null;
            DebugDrawCharSpanBoundsColor = null;
            DebugDrawLineBoundsColor = null;

            if (IsInDebugMode)
            {
                TextEditor.DebugReRender();
            }
        }

        #region 四线格

        /// <summary>
        /// 是否显示四线格的调试信息
        /// </summary>
        public bool ShowHandwritingPaperDebugInfo
        {
            get => _showHandwritingPaperDebugInfo && IsInDebugMode;
            set => _showHandwritingPaperDebugInfo = value;
        }
        private bool _showHandwritingPaperDebugInfo;

        public HandwritingPaperDebugDrawInfo HandwritingPaperDebugDrawInfo { get; set; }

        /// <summary>
        /// 调试模式下显示四线格的调试信息
        /// </summary>
        public void ShowHandwritingPaperDebugInfoWhenInDebugMode()
        {
            if (!IsInDebugMode)
            {
                return;
            }
            ShowHandwritingPaperDebugInfo = true;
            HandwritingPaperDebugDrawInfo = new HandwritingPaperDebugDrawInfo
            {
                TopLineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Red, 2),
                MiddleLineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Red, 2),
                BaselineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Red, 3),
                BottomLineGradationDebugDrawInfo = new HandwritingPaperGradationDebugDrawInfo(SKColors.Red, 2)
            };
            TextEditor.DebugReRender();
        }
        #endregion
    }
}
