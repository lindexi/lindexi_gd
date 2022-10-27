using System;

namespace LightTextEditorPlus.Core.Exceptions;

public class TextEditorRenderInfoDirtyException : Exception
{
    public override string Message => "文本布局已更新，此渲染信息是脏的";
}