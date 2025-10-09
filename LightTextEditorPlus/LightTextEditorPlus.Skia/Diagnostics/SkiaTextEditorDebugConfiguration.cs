using LightTextEditorPlus.Core.Diagnostics;
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

        /// <inheritdoc cref="TextEditorDebugConfiguration.IsInDebugMode"/>
        public bool IsInDebugMode => TextEditor.TextEditorCore.IsInDebugMode;

        private SkiaTextEditor TextEditor { get; }

        /// <summary>
        /// 调试绘制每个字符边框的颜色
        /// </summary>
        /// <remarks>设置为 null 则代表不显示</remarks>
        public TextEditorDebugBoundsDrawInfo? DebugDrawCharBoundsInfo { get; set; }

        /// <summary>
        /// 调试绘制每个连续字符 Span 的边界的颜色
        /// </summary>
        /// <remarks>设置为 null 则代表不显示</remarks>
        public TextEditorDebugBoundsDrawInfo? DebugDrawCharSpanBoundsInfo { get; set; }

        /// <summary>
        /// 调试绘制每行的边界的颜色
        /// </summary>
        /// <remarks>设置为 null 则代表不显示</remarks>
        public TextEditorDebugBoundsDrawInfo? DebugDrawLineContentBoundsInfo { get; set; }

        /// <summary>
        /// 调试绘制每行的轮廓边界的颜色
        /// </summary>
        public TextEditorDebugBoundsDrawInfo? DebugDrawLineOutlineBoundsInfo { get; set; }

        /// <summary>
        /// 调试绘制每个段落的内容边界的颜色
        /// </summary>
        public TextEditorDebugBoundsDrawInfo? DebugDrawParagraphContentBoundsInfo { get; set; }
        /// <summary>
        /// 调试绘制每个段落的轮廓边界的颜色
        /// </summary>
        public TextEditorDebugBoundsDrawInfo? DebugDrawParagraphOutlineBoundsInfo { get; set; }
        /// <summary>
        /// 调试绘制整个文档的渲染边界的颜色
        /// </summary>
        public TextEditorDebugBoundsDrawInfo? DebugDrawDocumentRenderBoundsInfo { get; set; }
        /// <summary>
        /// 调试绘制整个文档的内容边界的颜色
        /// </summary>
        public TextEditorDebugBoundsDrawInfo? DebugDrawDocumentContentBoundsInfo { get; set; }
        /// <summary>
        /// 调试绘制整个文档的轮廓边界的颜色
        /// </summary>
        public TextEditorDebugBoundsDrawInfo? DebugDrawDocumentOutlineBoundsInfo { get; set; }

        // 现在的赋值属性太多了，不如交给 Demo 来设置，让设置能够和调试界面相同
        ///// <summary>
        ///// 在进入调试模式下，显示所有的调试边框。如 <see cref="IsInDebugMode"/> 不为 true 则不会生效
        ///// </summary>
        //public void ShowAllDebugBoundsWhenInDebugMode()
        //{
        //    if (!IsInDebugMode)
        //    {
        //        return;
        //    }

        //    DebugDrawCharBoundsColor = SKColors.CadetBlue.WithAlpha(0xA0);
        //    DebugDrawCharSpanBoundsColor = SKColors.Red.WithAlpha(0xA0);
        //    DebugDrawLineBoundsColor = SKColors.Blue.WithAlpha(0x50);

        //    TextEditor.DebugReRender();
        //}

        /// <inheritdoc cref="SkiaTextEditor.DebugReRender"/>
        public void DebugReRender()
        {
            TextEditor.DebugReRender();
        }

        /// <summary>
        /// 清理所有的调试边框
        /// </summary>
        public void ClearAllDebugBounds()
        {
            DebugDrawCharBoundsInfo = null;
            DebugDrawCharSpanBoundsInfo = null;
            DebugDrawLineContentBoundsInfo = null;
            DebugDrawLineOutlineBoundsInfo = null;
            DebugDrawParagraphContentBoundsInfo = null;
            DebugDrawParagraphOutlineBoundsInfo = null;
            DebugDrawDocumentRenderBoundsInfo = null;
            DebugDrawDocumentContentBoundsInfo = null;
            DebugDrawDocumentOutlineBoundsInfo = null;

            if (IsInDebugMode)
            {
                DebugReRender();
            }
        }

        #region 四线格

        /// <summary>
        /// 是否显示四线格的调试信息
        /// </summary>
        public bool ShowHandwritingPaperDebugInfo
        {
            get => field && IsInDebugMode;
            set;
        }

        /// <summary>
        /// 四线格的调试绘制信息
        /// </summary>
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

        /// <summary>
        /// 隐藏四线格的调试信息
        /// </summary>
        public void HideHandwritingPaperDebugInfoWhenInDebugMode()
        {
            if (!IsInDebugMode)
            {
                return;
            }
            ShowHandwritingPaperDebugInfo = false;
            TextEditor.DebugReRender();
        }

        #endregion
    }
}
