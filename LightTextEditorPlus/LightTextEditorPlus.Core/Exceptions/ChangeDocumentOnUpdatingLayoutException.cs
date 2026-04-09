namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 在更新布局的过程中修改文档的异常
/// </summary>
public class ChangeDocumentOnUpdatingLayoutException : TextEditorException
{
    internal ChangeDocumentOnUpdatingLayoutException(TextEditorCore textEditor) : base(textEditor)
    {
    }

    /// <inheritdoc />
    public override string Message => $"在更新布局的过程中修改文档，此时将会导致布局错误。TextEditor={TextEditor}";
}