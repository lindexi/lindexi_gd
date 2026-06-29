namespace PptxGenerator.Models;

/// <summary>
/// SlideML 解析过程中发生的异常的基类。
/// </summary>
public abstract class SlideMlParseException : Exception
{
    /// <summary>
    /// 初始化 <see cref="SlideMlParseException"/> 类的新实例。
    /// </summary>
    protected SlideMlParseException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="SlideMlParseException"/> 类的新实例。
    /// </summary>
    protected SlideMlParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="SlideMlParseException"/> 类的新实例。
    /// </summary>
    protected SlideMlParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// SlideML 根元素相关错误时引发的异常。
/// </summary>
public class SlideMlRootElementException : SlideMlParseException
{
    public SlideMlRootElementException() { }
    public SlideMlRootElementException(string message) : base(message) { }
    public SlideMlRootElementException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 遇到不支持的标签时引发的异常。
/// </summary>
public class SlideMlUnsupportedElementException : SlideMlParseException
{
    public string? TagName { get; }

    public SlideMlUnsupportedElementException() { }
    public SlideMlUnsupportedElementException(string message) : base(message) { }
    public SlideMlUnsupportedElementException(string message, string tagName) : base(message) { TagName = tagName; }
    public SlideMlUnsupportedElementException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 必填属性缺失时引发的异常。
/// </summary>
public class SlideMlRequiredAttributeMissingException : SlideMlParseException
{
    public string? ElementId { get; }
    public string? AttributeName { get; }

    public SlideMlRequiredAttributeMissingException() { }
    public SlideMlRequiredAttributeMissingException(string message) : base(message) { }
    public SlideMlRequiredAttributeMissingException(string message, string? elementId, string? attributeName)
        : base(message)
    {
        ElementId = elementId;
        AttributeName = attributeName;
    }
    public SlideMlRequiredAttributeMissingException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 属性值格式错误时引发的异常（如数值解析失败或枚举值无效）。
/// </summary>
public class SlideMlAttributeFormatException : SlideMlParseException
{
    public string? ElementId { get; }
    public string? AttributeName { get; }
    public string? RawValue { get; }

    public SlideMlAttributeFormatException() { }
    public SlideMlAttributeFormatException(string message) : base(message) { }
    public SlideMlAttributeFormatException(string message, Exception innerException) : base(message, innerException) { }
    public SlideMlAttributeFormatException(string message, string? elementId, string? attributeName, string? rawValue)
        : base(message)
    {
        ElementId = elementId;
        AttributeName = attributeName;
        RawValue = rawValue;
    }
    public SlideMlAttributeFormatException(string message, string? elementId, string? attributeName, string? rawValue, Exception innerException)
        : base(message, innerException)
    {
        ElementId = elementId;
        AttributeName = attributeName;
        RawValue = rawValue;
    }
}
