using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core;

// 此文件存放调试相关的方法
partial class TextEditorCore
{
    #region 调试属性

    /// <summary>
    /// 日志
    /// </summary>
    public ITextLogger Logger { get; }

    /// <summary>
    /// 这个文本的调试名，用于在各个抛出的异常等，方便记录调试日志或埋点上报了解是哪个文本框抛出的。默认是空将取文本的前15个字符
    /// </summary>
    public string? DebugName
    {
        set => _debugName = value;
        get
        {
            if (_debugName is null)
            {
                return "\"" + this.GetText().LimitTrim(15) + "\"";
            }

            return _debugName;
        }
    }

    private string? _debugName;

    /// <inheritdoc />
    public override string ToString() => $"[{nameof(TextEditorCore)}] {DebugName}";

    /// <summary>
    /// 文本调试的配置
    /// </summary>
    public TextEditorDebugConfiguration DebugConfiguration { get; }

    /// <inheritdoc cref="TextEditorDebugConfiguration.IsInDebugMode"/>
    public bool IsInDebugMode => DebugConfiguration.IsInDebugMode;

    /// <inheritdoc cref="TextEditorDebugConfiguration.SetInDebugMode"/>
    public void SetInDebugMode() => DebugConfiguration.SetInDebugMode();

    /// <inheritdoc cref="TextEditorDebugConfiguration.SetAllInDebugMode"/>
    public static void SetAllInDebugMode() => TextEditorDebugConfiguration.SetAllInDebugMode();

    #endregion

    #region 更新布局原因

    /// <summary>
    /// 添加触发布局的原因，仅仅设置 <see cref="IsInDebugMode"/> 有效
    /// </summary>
    /// <param name="reason"></param>
    public void AddLayoutReason(string reason)
    {
        if (IsInDebugMode)
        {
            _layoutUpdateReasonManager ??= new LayoutUpdateReasonManager(this);
            _layoutUpdateReasonManager.AddLayoutReason(reason);
            Logger.LogDebug($"[TextEditorCore][AddLayoutReason] 添加布局理由 {reason}");
        }
    }

    /// <inheritdoc cref="AddLayoutReason(string)"/>
    public void AddLayoutReason(DefaultInterpolatedStringHandler reason)
    {
        if (IsInDebugMode)
        {
            AddLayoutReason(reason.ToStringAndClear());
        }
    }

    private LayoutUpdateReasonManager? _layoutUpdateReasonManager;

    internal string? GetLayoutUpdateReason() => _layoutUpdateReasonManager?.ReasonText;

    #endregion
}
