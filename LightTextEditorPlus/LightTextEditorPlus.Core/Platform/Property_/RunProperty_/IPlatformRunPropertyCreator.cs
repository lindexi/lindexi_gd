using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的字符属性创建器
/// </summary>
public interface IPlatformRunPropertyCreator
{
    /// <summary>
    /// 获取默认的字符属性
    /// </summary>
    /// <returns></returns>
    IReadOnlyRunProperty GetDefaultRunProperty();

    /// <summary>
    /// 获取字符属性。需要处理字符的字体降级
    /// 这个方法包含两个功能：
    /// 1. 如果传入的平台属性不属于当前的平台属性，则自动进行处理或记录或报告错误
    /// 2. 如果给定字符不能满足当前的平台属性，则自动处理字符的字体降级、或记录或报告错误
    /// </summary>
    /// <param name="charObject"></param>
    /// <param name="baseRunProperty"></param>
    /// <returns></returns>
    IReadOnlyRunProperty ToPlatformRunProperty(ICharObject charObject, IReadOnlyRunProperty baseRunProperty);

    /// <summary>
    /// 更新项目符号的字符属性
    /// </summary>
    /// <param name="markerRunProperty">当前项目符号的属性，可能有些属性要保留，如字体</param>
    /// <param name="styleRunProperty"></param>
    /// <returns></returns>
    IReadOnlyRunProperty UpdateMarkerRunProperty(IReadOnlyRunProperty? markerRunProperty, IReadOnlyRunProperty styleRunProperty);
}
