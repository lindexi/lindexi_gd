using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document.UndoRedo;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core;

partial class TextEditorCore
{
    #region UndoRedo

    /// <inheritdoc cref="T:LightTextEditorPlus.Core.Document.UndoRedo.ITextEditorUndoRedoProvider"/>
    public ITextEditorUndoRedoProvider UndoRedoProvider =>
        _undoRedoProvider ??= PlatformProvider.BuildTextEditorUndoRedoProvider();
    private ITextEditorUndoRedoProvider? _undoRedoProvider;

    /// <summary>
    /// 进入撤销恢复模式
    /// </summary>
    /// 由于撤销恢复需要一些绕过安全的步骤，因此需要先设置开关才能调用。同时撤销重做需要处理在撤销或恢复过程产生动作
    public void EnterUndoRedoMode()
    {
        IsUndoRedoMode = true;
    }

    /// <summary>
    /// 退出撤销恢复模式
    /// </summary>
    public void QuitUndoRedoMode()
    {
        IsUndoRedoMode = false;
    }

    /// <summary>
    /// 获取文本是否进入撤销恢复模式
    /// </summary>
    public bool IsUndoRedoMode { private set; get; }

    /// <summary>
    /// 撤销恢复是否可用
    /// </summary>
    public bool EnableUndoRedo { private set; get; } = true;

    /// <summary>
    /// 设置撤销恢复是否可用
    /// </summary>
    /// <remarks>业务方自行确保成对的问题。文本撤销恢复采用的是增量方式，如果是开启撤销恢复-写入历史记录-关闭撤销恢复-执行文本变更-开启撤销恢复-写入历史记录，那么撤销恢复栈将记录不连续的内容，可能导致文本无法正确进行撤销恢复</remarks>
    /// <param name="isEnable"></param>
    /// <param name="debugReason">调试使用的设置撤销恢复的理由</param>
    /// <returns></returns>
    public void SetUndoRedoEnable(bool isEnable, string debugReason)
    {
        if (IsInDebugMode)
        {
            Logger.LogDebug($"[SetUndoRedoEnable] Enable={isEnable};Reason={debugReason}");
        }

        EnableUndoRedo = isEnable;
    }

    /// <summary>
    /// 是否应该插入撤销恢复。等同于不在撤销恢复模式且撤销恢复可用
    /// </summary>
    internal bool ShouldInsertUndoRedo => !IsUndoRedoMode && EnableUndoRedo;

    /// <summary>
    /// 判断当前是否进入撤销恢复模式，如果没有，抛出 <see cref="TextEditorNotInUndoRedoModeException"/> 异常
    /// </summary>
    /// <exception cref="TextEditorNotInUndoRedoModeException"></exception>
    public void VerifyInUndoRedoMode()
    {
        if (!IsUndoRedoMode)
        {
            throw new TextEditorNotInUndoRedoModeException(this);
        }
    }

    #endregion
}
