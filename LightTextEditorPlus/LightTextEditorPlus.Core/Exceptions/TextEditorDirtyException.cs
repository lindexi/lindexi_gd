using System;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 表示当前的文本被更改，还没有完成布局渲染，不能获取渲染布局相关内容
/// </summary>
public class TextEditorDirtyException : TextEditorException
{
    internal TextEditorDirtyException(TextEditorCore textEditor) : base(textEditor)
    {
    }

    /// <inheritdoc />
    public override string Message => "当前的文本被更改，还没有完成布局渲染，不能获取渲染布局相关内容";
}