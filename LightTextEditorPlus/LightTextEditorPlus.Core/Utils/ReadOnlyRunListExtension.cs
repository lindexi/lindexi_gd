using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Utils;

static class ReadOnlyRunListExtension
{
    public static (ICharObject charObject, IReadOnlyRunProperty? RunProperty) GetCharInfo(
        this in TextReadOnlyListSpan<IImmutableRun> runList, int charIndex)
    {
        var (run, _, hitIndex) = runList.GetRunByCharIndex(charIndex);
        return (run.GetChar(hitIndex), run.RunProperty);
    }

    /// <summary>
    /// 给定相对于当前的 <paramref name="runList"/> 的 <paramref name="charIndex"/> 字符序号，获取到字符所落在的 <see cref="IImmutableRun"/> 里面，以及此 <see cref="IImmutableRun"/> 所在相对于的 <paramref name="runList"/> 的元素下标序号，以及给定的 <paramref name="charIndex"/> 字符序号在此 <see cref="IImmutableRun"/> 里面的序号
    /// </summary>
    /// <param name="runList"></param>
    /// <param name="charIndex"></param>
    /// <returns>
    /// run：字符所落在的 <see cref="IImmutableRun"/> 里面
    /// runIndex：此 <see cref="IImmutableRun"/> 所在相对于的 <paramref name="runList"/> 的元素下标序号
    /// hitIndex：给定的 <paramref name="charIndex"/> 字符序号在此 <see cref="IImmutableRun"/> 里面的序号
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">给定的字符超过 <paramref name="runList"/> 的字符数量</exception>
    public static (IImmutableRun run, int runIndex, int hitIndex) GetRunByCharIndex(this in TextReadOnlyListSpan<IImmutableRun> runList, int charIndex)
    {
        var currentCharCount = 0;
        for (var i = 0; i < runList.Count; i++)
        {
            var run = runList[i];
            var length = run.Count;
            var behindOffset = currentCharCount + length;

            // 判断是否落在当前的里面
            if (behindOffset >= charIndex)
            {
                var hitIndex = charIndex - currentCharCount;
                var runIndex = i;
                return (run, runIndex, hitIndex);
            }
            else
            {
                currentCharCount = behindOffset;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(charIndex), $"最大的字符数量是: {currentCharCount}；传入的字符索引是: {charIndex}");
    }
}