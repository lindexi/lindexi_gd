namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个不可变的对象值
/// </summary>
public interface IImmutableRunPropertyValue
{
}

/// <summary>
/// 表示一个不可变的对象值
/// </summary>
/// 要是还有人去拿属性去改，那我也救不了了
public class ImmutableRunPropertyValue<T> : IImmutableRunPropertyValue
{
    public ImmutableRunPropertyValue(T value)
    {
        Value = value;
    }

    public T Value { get; }
}