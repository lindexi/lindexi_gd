using LightTextEditorPlus.Core.Document.UndoRedo;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 程序集内使用的扩展
/// </summary>
static class TextEditorInternalExtension
{
    /// <summary>
    /// 插入撤销恢复内容 如果文本处于撤销恢复模式则不插入
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    public static TextEditorCore InsertUndoRedoOperation(this TextEditorCore textEditor,ITextOperation operation)
    {
        if (textEditor.IsUndoRedoMode)
        {
            // 如果文本处于撤销恢复模式则不插入
        }
        else
        {
            textEditor.UndoRedoProvider.Insert(operation);
        }

        return textEditor;
    }
}