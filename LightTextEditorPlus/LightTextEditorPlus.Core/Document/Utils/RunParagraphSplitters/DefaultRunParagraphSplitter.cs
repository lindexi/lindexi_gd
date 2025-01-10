using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document.Utils;

internal class DefaultRunParagraphSplitter : IRunParagraphSplitter
{
    public IEnumerable<IImmutableRun> Split(IImmutableRun run)
    {
        if (run is TextRun textRun)
        {
            return Split(textRun);

            // 现在也是遍历，就不需要再判断是否包含换行符了
            //var text = textRun.Text;

            //if (text.Contains('\n') || text.Contains('\r'))
            //{
            //    return Split(textRun);
            //}
            //else
            //{
            //    // 这是一个优化，大部分的文本都没有包含换行的输入，那就返回自身即可，不需要再构建复杂的逻辑
            //    return [textRun];
            //}
        }
        else if (run is SingleCharImmutableRun singleCharImmutableRun)
        {
            return [singleCharImmutableRun];
        }
        else
        {
            return [run];
        }
    }

    private static IEnumerable<IImmutableRun> Split(TextRun textRun)
    {
        var start = -1;
        var length = 0;
        // 对于文本来说，需要考虑 123\r\n 和 123\r\nabc 的情况，这两个情况都是两段
        // 但 123\r\n\r\n 和 123\r\n\r\nabc 的情况是三段
        bool endWithBreakLine = false;
        for (int i = 0; i < textRun.Count; i++)
        {
            var charObject = textRun.GetChar(i);
            if (ReferenceEquals(charObject, LineBreakCharObject.Instance))
            {
                if (length > 0)
                {
                    Debug.Assert(start >= 0);
                    yield return textRun.Slice(start, length);
                    start = -1;
                    length = 0;
                    endWithBreakLine = true;
                }
                else
                {
                    // 如果是 123\r\nabc 则需要返回两段，分别是 123 和 abc 两段
                    // 如果是 123\r\n\r\nabc则需要返回三段，分别是 123、LineBreakRun、abc 三段
                    // 因此判断 length > 0 即可知道是否多余了一行
                    yield return new LineBreakRun(textRun.RunProperty);
                }
            }
            else
            {
                if (start == -1)
                {
                    start = i;
                    length = 1;
                }
                else
                {
                    length++;
                }

                endWithBreakLine = false;
            }
        }

        if (length == textRun.Count)
        {
            yield return textRun;
        }
        else if (length > 0)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(endWithBreakLine is false);

            yield return textRun.Slice(start, length);
        }

        if (endWithBreakLine)
        {
            Debug.Assert(length == 0);
            yield return new LineBreakRun(textRun.RunProperty);
        }
    }
}
