using System;

namespace PptxGenerator;

/// <summary>
/// SlideML 解析过程中发生的异常的基类。
/// </summary>
public abstract class SlideMlParseException : InvalidOperationException
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
    /// <param name="message">描述错误的消息。</param>
    protected SlideMlParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="SlideMlParseException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
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
    /// <summary>
    /// 初始化 <see cref="SlideMlRootElementException"/> 类的新实例。
    /// </summary>
    public SlideMlRootElementException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="SlideMlRootElementException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public SlideMlRootElementException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="SlideMlRootElementException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public SlideMlRootElementException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 遇到不支持的标签时引发的异常。
/// </summary>
public class SlideMlUnsupportedElementException : SlideMlParseException
{
    /// <summary>
    /// 获取不支持的标签名称。
    /// </summary>
    public string? TagName { get; }

    /// <summary>
    /// 初始化 <see cref="SlideMlUnsupportedElementException"/> 类的新实例。
    /// </summary>
    public SlideMlUnsupportedElementException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="SlideMlUnsupportedElementException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public SlideMlUnsupportedElementException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息和不支持的标签名称初始化 <see cref="SlideMlUnsupportedElementException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="tagName">不支持的标签名称。</param>
    public SlideMlUnsupportedElementException(string message, string tagName)
        : base(message)
    {
        TagName = tagName;
    }

    /// <summary>
    /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="SlideMlUnsupportedElementException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public SlideMlUnsupportedElementException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 必填属性缺失时引发的异常。
/// </summary>
public class SlideMlRequiredAttributeMissingException : SlideMlParseException
{
    /// <summary>
    /// 获取元素的 ID（如果存在）。
    /// </summary>
    public string? ElementId { get; }

    /// <summary>
    /// 获取缺失的属性名称。
    /// </summary>
    public string? AttributeName { get; }

    /// <summary>
    /// 初始化 <see cref="SlideMlRequiredAttributeMissingException"/> 类的新实例。
    /// </summary>
    public SlideMlRequiredAttributeMissingException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="SlideMlRequiredAttributeMissingException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public SlideMlRequiredAttributeMissingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息、元素 ID 和属性名称初始化 <see cref="SlideMlRequiredAttributeMissingException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="elementId">元素的 ID。</param>
    /// <param name="attributeName">缺失的属性名称。</param>
    public SlideMlRequiredAttributeMissingException(string message, string? elementId, string? attributeName)
        : base(message)
    {
        ElementId = elementId;
        AttributeName = attributeName;
    }

    /// <summary>
    /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="SlideMlRequiredAttributeMissingException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public SlideMlRequiredAttributeMissingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// 属性值格式错误时引发的异常（如数值解析失败或枚举值无效）。
/// </summary>
public class SlideMlAttributeFormatException : SlideMlParseException
{
    /// <summary>
    /// 获取元素的 ID（如果存在）。
    /// </summary>
    public string? ElementId { get; }

    /// <summary>
    /// 获取格式错误的属性名称。
    /// </summary>
    public string? AttributeName { get; }

    /// <summary>
    /// 获取导致格式错误的原始值。
    /// </summary>
    public string? RawValue { get; }

    /// <summary>
    /// 初始化 <see cref="SlideMlAttributeFormatException"/> 类的新实例。
    /// </summary>
    public SlideMlAttributeFormatException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="SlideMlAttributeFormatException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    public SlideMlAttributeFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="SlideMlAttributeFormatException"/> 类的新实例。
    /// </summary>
    /// <param name="message">解释异常原因的消息。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public SlideMlAttributeFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用指定的错误消息、元素 ID、属性名称和原始值初始化 <see cref="SlideMlAttributeFormatException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="elementId">元素的 ID。</param>
    /// <param name="attributeName">格式错误的属性名称。</param>
    /// <param name="rawValue">导致格式错误的原始值。</param>
    public SlideMlAttributeFormatException(string message, string? elementId, string? attributeName, string? rawValue)
        : base(message)
    {
        ElementId = elementId;
        AttributeName = attributeName;
        RawValue = rawValue;
    }

    /// <summary>
    /// 使用指定的错误消息、元素 ID、属性名称、原始值和内部异常初始化 <see cref="SlideMlAttributeFormatException"/> 类的新实例。
    /// </summary>
    /// <param name="message">描述错误的消息。</param>
    /// <param name="elementId">元素的 ID。</param>
    /// <param name="attributeName">格式错误的属性名称。</param>
    /// <param name="rawValue">导致格式错误的原始值。</param>
    /// <param name="innerException">导致当前异常的异常。</param>
    public SlideMlAttributeFormatException(string message, string? elementId, string? attributeName, string? rawValue, Exception innerException)
        : base(message, innerException)
    {
        ElementId = elementId;
        AttributeName = attributeName;
        RawValue = rawValue;
    }
}
