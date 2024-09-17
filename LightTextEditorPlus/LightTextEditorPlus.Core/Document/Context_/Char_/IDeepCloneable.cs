namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个类型支持深拷贝
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDeepCloneable<out T>
{
    /// <summary>
    /// 深拷贝
    /// </summary>
    /// <returns></returns>
    T DeepClone();
}