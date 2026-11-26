using Microsoft.Extensions.AI;

namespace PptxGenerator.Rendering;

/// <summary>提供当前渲染环境可用的附加 SlideML 工具。</summary>
public interface ISlideMlToolProvider
{
    /// <summary>获取当前可用工具的快照。</summary>
    IReadOnlyList<AIFunction> SlideMlTools { get; }
}
