namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个类型支持段落的缓存，且支持缓存失效
/// </summary>
interface IParagraphCache
{
    /// <summary>
    /// 当前段落的版本，段落变更的时候就会自动加段落的版本号，一旦发现此属性和段落的版本不相同，那就证明当前的缓存失效
    /// </summary>
    uint CurrentParagraphVersion { set; get; }
}