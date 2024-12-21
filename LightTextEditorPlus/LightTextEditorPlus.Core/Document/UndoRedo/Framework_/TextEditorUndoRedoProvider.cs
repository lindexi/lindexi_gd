using System.Collections.Generic;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core.Document.UndoRedo;

/// <summary>
/// 默认的空白文本撤销恢复提供
/// </summary>
public class TextEditorUndoRedoProvider : ITextEditorUndoRedoProvider
{
    /// <inheritdoc />
    public void Insert(ITextOperation textOperation)
    {
        OnInsert(textOperation);
    }

    /// <inheritdoc cref="Insert"/>
    protected virtual void OnInsert(ITextOperation operation)
    {
        if (_isUndoRedoing)
        {
            throw new TextEditorUndoRedoReentrantException();
        }

        _currentUndoRedoStack.UndoStack.Push(operation);
        _currentUndoRedoStack.RedoStack.Clear();
    }

    /// <summary>
    /// 执行撤消操作
    /// </summary>
    public void Undo()
    {
        _isUndoRedoing = true;
        try
        {
            if (_currentUndoRedoStack.UndoStack.Count == 0)
            {
                return;
            }

            var operation = _currentUndoRedoStack.UndoStack.Pop();
            operation.Undo();
            _currentUndoRedoStack.RedoStack.Push(operation);
        }
        finally
        {
            _isUndoRedoing = false;
        }
    }

    /// <summary>
    /// 执行恢复操作
    /// </summary>
    public void Redo()
    {
        _isUndoRedoing = true;
        try
        {
            if (_currentUndoRedoStack.RedoStack.Count == 0)
            {
                return;
            }

            var operation = _currentUndoRedoStack.RedoStack.Pop();
            operation.Redo();
            _currentUndoRedoStack.UndoStack.Push(operation);
        }
        finally
        {
            _isUndoRedoing = false;
        }
    }

    /// <summary>
    /// 获取撤消是否可用
    /// </summary>
    public bool CanUndo => _currentUndoRedoStack.UndoStack.Count != 0;

    /// <summary>
    /// 获取恢复是否可用
    /// </summary>
    public bool CanRedo => _currentUndoRedoStack.RedoStack.Count != 0;

    /// <summary>
    /// 当前操作的撤消恢复栈
    /// </summary>
    private readonly TextEditorUndoRedoStack _currentUndoRedoStack = new TextEditorUndoRedoStack();

    private bool _isUndoRedoing;

}

/// <summary>
/// 撤消恢复栈组，用于应用程序或文档的撤消恢复功能实现。
/// </summary>
class TextEditorUndoRedoStack
{
    /// <summary>
    /// 获取撤消操作栈
    /// </summary>
    public Stack<ITextOperation> UndoStack { get; } = new();

    /// <summary>
    /// 获取恢复操作栈
    /// </summary>
    public Stack<ITextOperation> RedoStack { get; } = new();

}
