//using System;
//using LightTextEditorPlus.Core.Document;
//using LightTextEditorPlus.Core.Primitive.Collections;
//using LightTextEditorPlus.Core.Utils;

//namespace LightTextEditorPlus.Core.Layout;

//public readonly record struct RunInfo(ReadOnlyListSpan<IImmutableRun> RunList, int CurrentIndex,
//    int CurrentCharHitIndex, IReadOnlyRunProperty DefaultRunProperty)
//{
//    public CharInfo GetCurrentCharInfo()
//    {
//        var run = RunList[CurrentIndex];
//        var charObject = run.GetChar(CurrentCharHitIndex);
//        return new CharInfo(charObject, run.RunProperty ?? DefaultRunProperty);
//    }

//    public CharInfo GetNextCharInfo(int index = 1)
//    {
//        if (index > 0)
//        {
//            var charIndex = CurrentCharHitIndex + index;

//            var currentRun = RunList[CurrentIndex];
//            if (charIndex < currentRun.Count)
//            {
//                // 这是一个优化，判断是否在当前的文本段内

//                return new CharInfo(currentRun.GetChar(charIndex), currentRun.RunProperty ?? DefaultRunProperty);
//            }

//            // 从当前开始进行拆分，拆分之后即可相对于当前的索引开始计算
//            var runSpan = RunList.Slice(CurrentIndex);

//            var (charObject, runProperty) = runSpan.GetCharInfo(charIndex);

//            return new CharInfo(charObject, runProperty ?? DefaultRunProperty);
//        }
//        else if (index == 0)
//        {
//            throw new ArgumentException($"请使用 {nameof(GetCurrentCharInfo)} 方法代替", nameof(index));
//        }
//        else
//        {
//            // 计算出 index 的相对于 RunList 的字符序号是多少
//            // 然后对整个进行计算获取到对应的字符
//            var charIndex = 0;
//            for (var i = 0; i < CurrentIndex; i++)
//            {
//                var run = RunList[i];
//                charIndex += run.Count;
//            }

//            charIndex += CurrentCharHitIndex;
//            charIndex += index;

//            var (charObject, runProperty) = RunList.GetCharInfo(charIndex);
//            return new CharInfo(charObject, runProperty ?? DefaultRunProperty);
//        }
//    }
//}