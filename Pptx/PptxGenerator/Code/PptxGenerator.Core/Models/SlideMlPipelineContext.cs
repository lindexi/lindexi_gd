using PptxGenerator.Rendering.Materials;

namespace PptxGenerator.Models;

/// <summary>
/// 渲染流水线共享上下文，包含诊断信息收集与资源管理。
/// 画布尺寸等不可变配置通过 <see cref="SlideDocumentContext"/> 属性暴露。
/// 仅允许通过 <see cref="AddWarning"/> / <see cref="AddError"/> 追加，
/// 通过 <see cref="Reset"/> 清空，外部不可直接修改集合。
/// </summary>
public sealed class SlideMlPipelineContext
{
    private readonly List<string> _warnings = new();
    private readonly List<string> _errors = new();

    /// <summary>
    /// 初始化 <see cref="SlideMlPipelineContext"/> 的新实例，使用默认画布尺寸。
    /// </summary>
    public SlideMlPipelineContext() : this(slideDocumentContext: null)
    {
    }

    /// <summary>
    /// 初始化 <see cref="SlideMlPipelineContext"/> 的新实例并指定画布尺寸。
    /// </summary>
    /// <param name="canvasWidth">画布宽度。</param>
    /// <param name="canvasHeight">画布高度。</param>
    public SlideMlPipelineContext(int canvasWidth, int canvasHeight)
        : this(new SlideDocumentContext(canvasWidth, canvasHeight))
    {
    }

    /// <summary>
    /// 初始化 <see cref="SlideMlPipelineContext"/> 的新实例并指定文档上下文。
    /// </summary>
    /// <param name="slideDocumentContext">文档级上下文，包含画布尺寸等配置；为 <see langword="null"/> 时使用默认值。</param>
    public SlideMlPipelineContext(SlideDocumentContext? slideDocumentContext)
    {
        SlideDocumentContext = slideDocumentContext ?? new SlideDocumentContext();
    }

    /// <summary>
    /// 文档级上下文，包含画布尺寸等不可变的全局配置。
    /// </summary>
    public SlideDocumentContext SlideDocumentContext { get; }

    /// <summary>
    /// 渲染过程中的警告信息（只读）。
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// 渲染过程中的错误信息（只读）。
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// 添加一条警告信息。
    /// </summary>
    /// <param name="message">警告信息。</param>
    public void AddWarning(string message) => _warnings.Add(message);

    /// <summary>
    /// 添加多条警告信息。
    /// </summary>
    /// <param name="messages">警告信息集合。</param>
    public void AddWarnings(IEnumerable<string> messages) => _warnings.AddRange(messages);

    /// <summary>
    /// 添加一条错误信息。
    /// </summary>
    /// <param name="message">错误信息。</param>
    public void AddError(string message) => _errors.Add(message);

    /// <summary>
    /// 添加多条错误信息。
    /// </summary>
    /// <param name="messages">错误信息集合。</param>
    public void AddErrors(IEnumerable<string> messages) => _errors.AddRange(messages);

    /// <summary>
    /// 清空所有诊断信息，准备新一轮渲染。
    /// </summary>
    public void Reset()
    {
        _warnings.Clear();
        _errors.Clear();
    }

    public SlideMlMaterialResourceManager MaterialResourceManager { get; } = new();
}
