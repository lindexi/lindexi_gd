using System.Collections.Generic;

namespace PptxGenerator;

/// <summary>
/// 渲染流水线共享上下文，包含画布尺寸等全局配置以及诊断信息收集。
/// 仅允许通过 <see cref="AddWarning"/> / <see cref="AddError"/> 追加，
/// 通过 <see cref="Reset"/> 清空，外部不可直接修改集合。
/// </summary>
public sealed class SlidePipelineContext
{
    private readonly List<string> _warnings = new();
    private readonly List<string> _errors = new();

    /// <summary>
    /// 默认画布宽度。
    /// </summary>
    public const int DefaultCanvasWidth = 1280;

    /// <summary>
    /// 默认画布高度。
    /// </summary>
    public const int DefaultCanvasHeight = 720;

    /// <summary>
    /// 初始化 <see cref="SlidePipelineContext"/> 的新实例。
    /// </summary>
    public SlidePipelineContext()
    {
        CanvasWidth = DefaultCanvasWidth;
        CanvasHeight = DefaultCanvasHeight;
    }

    /// <summary>
    /// 初始化 <see cref="SlidePipelineContext"/> 的新实例并指定画布尺寸。
    /// </summary>
    /// <param name="canvasWidth">画布宽度。</param>
    /// <param name="canvasHeight">画布高度。</param>
    public SlidePipelineContext(int canvasWidth, int canvasHeight)
    {
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
    }

    /// <summary>
    /// 画布宽度（像素）。
    /// </summary>
    public int CanvasWidth { get; }

    /// <summary>
    /// 画布高度（像素）。
    /// </summary>
    public int CanvasHeight { get; }

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
}