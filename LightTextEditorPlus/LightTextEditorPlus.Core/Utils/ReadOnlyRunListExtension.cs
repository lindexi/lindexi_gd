﻿using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Utils;

static class ReadOnlyRunListExtension
{
    public static (ICharObject charObject, IReadOnlyRunProperty? RunProperty) GetCharInfo(
        this in ReadOnlyListSpan<IImmutableRun> runList, int charIndex)
    {
        var (run, _, hitIndex) = runList.GetRunByCharIndex(charIndex);
        return (run.GetChar(hitIndex), run.RunProperty);
    }

    public static (IImmutableRun run, int runIndex, int hitIndex) GetRunByCharIndex(this in ReadOnlyListSpan<IImmutableRun> runList, int charIndex)
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

        throw new ArgumentOutOfRangeException(nameof(charIndex),$"最大的字符数量是: {currentCharCount}；传入的字符索引是: {charIndex}");
    }
}