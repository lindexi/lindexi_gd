namespace LightTextEditorPlus.Core.Exceptions;

using LightTextEditorPlus.Core.Resources;

/// <summary>
/// 在更新布局的过程中修改文档的异常
/// </summary>
public class ChangeDocumentOnUpdatingLayoutException : TextEditorException
{
    internal ChangeDocumentOnUpdatingLayoutException(TextEditorCore textEditor) : base(textEditor)
    {
    }

    /// <inheritdoc />
    public override string Message =>
        ExceptionMessages.Format(nameof(ChangeDocumentOnUpdatingLayoutException) + "_Message", TextEditor);
}