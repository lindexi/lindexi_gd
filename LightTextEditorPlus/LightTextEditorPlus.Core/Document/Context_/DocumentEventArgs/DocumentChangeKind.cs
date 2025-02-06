namespace LightTextEditorPlus.Core.Document.DocumentEventArgs;

/// <summary>
/// 文档变更类型
/// </summary>
enum DocumentChangeKind
{
    /// <summary>
    /// 文本变更，如输入或删除等
    /// </summary>
    Text = 0B01,

    /// <summary>
    /// 样式变更，如字体、颜色等
    /// </summary>
    OnlyStyle = 0B10,
    ///// <summary>
    ///// 文本和样式变更
    ///// </summary>
    //TextAndStyle = 0B11/* 01|10=11=3 */,
}