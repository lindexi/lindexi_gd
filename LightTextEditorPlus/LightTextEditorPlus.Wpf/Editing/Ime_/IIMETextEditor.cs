using System.Windows;

namespace LightTextEditorPlus.Editing
{
    /// <summary>
    /// 表示控件支持被输入法
    /// </summary>
    interface IIMETextEditor
    {
        /// <summary>
        /// 获取当前使用的字体名
        /// </summary>
        /// <returns></returns>
        string GetFontFamilyName();

        /// <summary>
        /// 获取字号大小，单位和 WPF 的 FontSize 相同
        /// </summary>
        /// <returns></returns>
        int GetFontSize();

        /// <summary>
        /// 获取输入框的左上角的点，用于设置输入法的左上角。此点相对于 <see cref="IIMETextEditor"/> 所在元素坐标。对大部分控件来说，都应该是 0,0 点
        /// </summary>
        /// <returns></returns>
        Point GetTextEditorLeftTop();

        /// <summary>
        /// 获取光标的输入左上角的点。此点相对于 <see cref="IIMETextEditor"/> 所在元素坐标
        /// </summary>
        /// <returns></returns>
        Point GetCaretLeftTop();
    }
}