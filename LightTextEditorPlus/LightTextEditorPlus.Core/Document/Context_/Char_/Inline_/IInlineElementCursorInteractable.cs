using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 可选接口。实现此接口的内联元素允许光标进入其内部进行导航（如行内公式内部移动）。
/// 未实现此接口的内联元素按原子光标位处理：方向键直接跳过，不可进入内部。
/// </summary>
/// <remarks>
/// <para>此接口与 <see cref="Document.IInlineElementCharObject"/> 正交。一个内联元素可以同时实现这两个接口，
/// 也可以只实现 <see cref="Document.IInlineElementCharObject"/> 而不实现此接口（如图片）。</para>
/// <para>光标系统在方向键导航时，发现相邻 <see cref="Document.CharData"/> 是 inline 元素后，
/// 先检查其 CharObject 是否实现了此接口：</para>
/// <list type="bullet">
/// <item>未实现 → 按原子光标位处理，方向键跳过</item>
/// <item>实现了且 <see cref="CanCursorEnter"/> 为 true → 调用 <see cref="EnterCursor"/> 进入内部</item>
/// <item>实现了但 <see cref="CanCursorEnter"/> 为 false → 降级为原子光标位</item>
/// </list>
/// </remarks>
public interface IInlineElementCursorInteractable
{
    /// <summary>
    /// 当前是否允许光标进入（可能受只读状态或内部计算中的影响）。
    /// </summary>
    bool CanCursorEnter { get; }

    /// <summary>
    /// 光标进入该元素。从哪个方向进入决定了进入后的初始内部光标位置。
    /// </summary>
    /// <param name="direction">光标从哪个方向进入（如从左侧文本向右移动即 <see cref="Direction.Right"/>）</param>
    /// <param name="layoutInfo">布局信息（坐标、尺寸），由 Core 层提供</param>
    /// <returns>进入后的内部光标状态。返回 null 表示拒绝进入，降级为原子光标位处理。</returns>
    InlineElementCursorState? EnterCursor(Direction direction, InlineElementLayoutInfo layoutInfo);

    /// <summary>
    /// 在元素内部移动光标。
    /// </summary>
    /// <param name="direction">移动方向</param>
    /// <param name="currentState">当前内部光标状态</param>
    /// <returns>
    /// 移动结果。若 <see cref="InlineElementCursorResult.NewState"/> 不为 null 则仍在内部；
    /// 若为 null 则表示光标已从 <see cref="InlineElementCursorResult.ExitDirection"/> 方向脱出该元素。
    /// </returns>
    InlineElementCursorResult MoveCursor(Direction direction, InlineElementCursorState currentState);

    /// <summary>
    /// 鼠标在元素内部点击的命中测试。
    /// </summary>
    /// <param name="localPoint">鼠标点击位置，相对于元素左上角</param>
    /// <param name="currentState">当前内部光标状态。null 表示当前光标不在该元素内部</param>
    /// <returns>点击后的光标状态。若脱出则 <see cref="InlineElementCursorResult.NewState"/> 为 null。</returns>
    InlineElementCursorResult HandleMouseClick(TextPoint localPoint, InlineElementCursorState? currentState);

    /// <summary>
    /// 获取当前内部光标状态对应的 Caret 渲染矩形（相对于元素左上角）。
    /// </summary>
    /// <param name="currentState">当前内部光标状态</param>
    /// <returns>Caret 在元素局部坐标系中的矩形</returns>
    TextRect GetCaretRect(InlineElementCursorState currentState);
}

/// <summary>
/// 内部光标状态。由 <see cref="IInlineElementCursorInteractable"/> 实现方定义具体类型，
/// Core 层将其作为不透明（opaque）状态透明传递，不解析内部结构。
/// </summary>
/// <remarks>
/// 光标脱出内联元素后，此状态立即丢弃。重新进入时，由 <see cref="IInlineElementCursorInteractable.EnterCursor"/>
/// 根据进入方向重新计算初始内部光标位置。
/// </remarks>
public abstract record InlineElementCursorState;

/// <summary>
/// 光标移动/点击的结果。表示光标仍在内联元素内部或已脱出。
/// </summary>
public readonly struct InlineElementCursorResult
{
    /// <summary>
    /// 移动/点击后的内部光标状态。null 表示光标已脱出该元素。
    /// </summary>
    public InlineElementCursorState? NewState { get; init; }

    /// <summary>
    /// 光标脱出方向。仅当 <see cref="NewState"/> 为 null 时有意义。
    /// </summary>
    public Direction? ExitDirection { get; init; }

    /// <summary>
    /// 创建仍在元素内部的结果。
    /// </summary>
    public static InlineElementCursorResult Stay(InlineElementCursorState newState)
        => new() { NewState = newState };

    /// <summary>
    /// 创建光标已脱出元素的结果。
    /// </summary>
    public static InlineElementCursorResult Exit(Direction exitDirection)
        => new() { ExitDirection = exitDirection };
}

/// <summary>
/// 内联元素布局信息。由 Core 布局层在光标进入时提供给 <see cref="IInlineElementCursorInteractable"/>。
/// </summary>
/// <param name="Bounds">内联元素在文档中的布局矩形（Canvas 坐标）</param>
public readonly record struct InlineElementLayoutInfo(TextRect Bounds);
