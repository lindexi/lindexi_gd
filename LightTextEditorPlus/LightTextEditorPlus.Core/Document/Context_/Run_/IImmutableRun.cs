namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一段不可变字符，具有相同的属性定义。表示共享单个属性集的字符序列
/// </summary>
/// 如果有人继承此接口，然而修改了此对象的字符数量或者包含的字符内容，那框架层也不会处理
public interface IImmutableRun
{
    /// <summary>
    /// 包含的字符数量，字符数量不等于 char 数量，而是等于人类语言的字符数量。例如表情是两个 char，但是只有一个字符
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取某个字符。返回的是一个 ICharObject 对象，这个对象是不可变的，返回的是人类语言的字符
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    ICharObject GetChar(int index);

    /// <summary>
    /// 定义的样式，可为空。空代表着没有定义特别的样式
    /// </summary>
    IReadOnlyRunProperty? RunProperty { get; }

    /// <summary>
    /// 将文本一分为二
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index);
}