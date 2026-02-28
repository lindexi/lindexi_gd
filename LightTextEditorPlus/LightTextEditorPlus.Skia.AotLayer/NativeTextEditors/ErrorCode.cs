namespace LightTextEditorPlus;

public readonly record struct ErrorCode(int Code, string Message)
{
    public static ErrorCode Success => new(0, "Success");

    public static ErrorCode TextEditorNotFound => new(-1, "找不到文本编辑器");

    public static ErrorCode TextEditorBeFree => new(-2, "文本编辑器已被释放");

    public static ErrorCode RunPropertyNotFound => new(-3, "找不到文本字符属性");

    public static ErrorCode RunPropertyBeFree => new(-4, "文本字符属性已被释放");

    public static implicit operator int(ErrorCode code) => code.Code;
}