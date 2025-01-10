//using System;
//using LightTextEditorPlus.Core.Document;
//using LightTextEditorPlus.Core.Primitive.Collections;
//using LightTextEditorPlus.Core.Utils;

//namespace LightTextEditorPlus.Core.Layout;

//// 这里无法确定采用字符加上属性的方式是否会更优
//// 通过字符获取对应的属性，如此即可不需要每次都需要考虑将
//// 一个 IImmutableRun 进行分割而已

//// 尝试根据字符给出属性的方式，如此可以不用考虑将一个 IImmutableRun 进行分割

//public class RunVisitor
//{
//    public RunVisitor(IReadOnlyRunProperty defaultRunProperty, ReadOnlyListSpan<IImmutableRun> runList)
//    {
//        DefaultRunProperty = defaultRunProperty;
//        RunList = runList;
//    }

//    public IReadOnlyRunProperty DefaultRunProperty { get; }
//    public ReadOnlyListSpan<IImmutableRun> RunList { get; }

//    public int CurrentCharIndex { get; private set; }

//    /// <summary>
//    /// 当前的 <see cref="RunIndex"/> 对应的字符起始点
//    /// </summary>
//    private int CharStartIndexOfCurrentRun { get; set; }

//    /// <summary>
//    /// 当前的 <see cref="RunList"/> 的序号
//    /// </summary>
//    public int RunIndex { get; set; }

//    public (ICharObject charObject, IReadOnlyRunProperty RunProperty) GetCurrentCharInfo()
//    {
//        var run = RunList[RunIndex];
//        var charObject = run.GetChar(CurrentCharIndex - CharStartIndexOfCurrentRun);
//        return (charObject, run.RunProperty ?? DefaultRunProperty);
//    }

//    public (ICharObject charObject, IReadOnlyRunProperty RunProperty) GetCharInfo(int charIndex)
//    {
//        var (charObject, runProperty) = RunList.GetCharInfo(charIndex);
//        return (charObject, runProperty ?? DefaultRunProperty);
//    }
//}