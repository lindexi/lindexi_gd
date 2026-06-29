using PptxGenerator.Models;
using PptxGenerator.Pipeline;

namespace PptxGenerator.Rendering;

/// <summary>
/// SlideML 渲染管道抽象接口，使 <see cref="SlideMlRenderTool"/> 不依赖具体的 UI 框架渲染实现。
/// 各 UI 框架（WPF、Avalonia）通过实现此接口来封装各自的渲染管道。
/// </summary>
public interface ISlideMlRenderPipeline
{
    /// <summary>
    /// 将 SlideML XML 渲染为预览图，并返回回填后的 XML 与警告信息。
    /// </summary>
    /// <param name="slideXml">SlideML XML 字符串。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>渲染结果。</returns>
    Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default);
}
