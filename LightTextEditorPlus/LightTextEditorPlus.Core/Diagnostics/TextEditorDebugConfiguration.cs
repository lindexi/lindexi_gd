using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Diagnostics;

/// <summary>
/// 调试配置信息
/// </summary>
public class TextEditorDebugConfiguration
{
    internal TextEditorDebugConfiguration(ITextLogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// 日志
    /// </summary>
    private ITextLogger Logger { get; }

    /// <summary>
    /// 是否正在调试模式
    /// </summary>
    /// 文本库将使用 Release 构建进行分发，但是依然提供调试方法，开启调试模式之后会有更多输出和判断逻辑，以及抛出调试异常。不应该在正式发布时，设置进入调试模式
    public bool IsInDebugMode
    {
        private set => _isInDebugMode = value;
        get => _isInDebugMode || IsAllInDebugMode;
    }

    private bool _isInDebugMode;

    /// <summary>
    /// 设置当前的文本进入调试模式
    /// </summary>
    internal void SetInDebugMode()
    {
        IsInDebugMode = true;
        Logger.LogInfo($"文本进入调试模式");
    }

    /// <summary>
    /// 是否全部的文本都进入调试模式
    /// </summary>
    public static bool IsAllInDebugMode { private set; get; }

    /// <summary>
    /// 设置全部的文本都进入调试模式，理论上不能将此调用此方法的代码进行发布
    /// </summary>
    internal static void SetAllInDebugMode()
    {
        // 这个方法是 internal 是因为设计上只允许从 TextEditorCore 入口进行设置，不允许直接拿到这个类进行设置
        // 这个类也可能传递给到其他模块，其他模块只读，不允许修改
        IsAllInDebugMode = true;
    }
}