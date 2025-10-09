using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Document;

static class CharCountHelper
{
    public static int GetCharCount(this IEnumerable<IImmutableRun> runs)
    {
        // 字符数量等于每段的字符数量之和，加上每个换行符的长度
        int charCount = 0;
        foreach (IImmutableRun immutableRun in runs)
        {
            if(immutableRun is LineBreakRun)
            {
                charCount += ParagraphData.DelimiterLength;
            }
            else
            {
                charCount += immutableRun.Count;
            }
        }

        return charCount;
    }
}