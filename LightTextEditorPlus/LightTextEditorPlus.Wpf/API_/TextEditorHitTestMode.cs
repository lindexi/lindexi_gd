using System.Windows.Controls;

namespace LightTextEditorPlus;

/// <summary>
/// 命中测试方式
/// </summary>
/// todo 考虑是否需要此类型
enum TextEditorHitTestMode
{
    /// <summary>
    /// 只有在文本框范围内，都视为命中。行为如 <see cref="TextBox"/> 这些
    /// </summary>
    All,

    /// <summary>
    /// 只有命中到有文字的地方才算命中到。也许没有什么业务会用到
    /// </summary>
    HitCharOnly,
}