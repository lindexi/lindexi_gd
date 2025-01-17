using System.Collections.Generic;
using System.Diagnostics;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

internal static class DocumentManagerRunPropertyExtension
{
    public static IEnumerable<IReadOnlyRunProperty> GetDifferentRunPropertyRangeInner(this IEnumerable<IReadOnlyRunProperty> runPropertyRange)
    {
        IReadOnlyRunProperty? lastRunProperty = null;

        foreach (var readOnlyRunProperty in runPropertyRange)
        {
            if (!ReferenceEquals(lastRunProperty, readOnlyRunProperty))
            {
                lastRunProperty = readOnlyRunProperty;
                yield return readOnlyRunProperty;
            }
        }
    }

    public static IEnumerable<IImmutableRun> GetImmutableRunRangeInner(this DocumentManager documentManager, Selection selection)
    {
        List<ICharObject>? currentCharObjectList = null;
        IReadOnlyRunProperty? lastChangedRunProperty = null;

        foreach (CharData charData in documentManager.GetCharDataRange(in selection))
        {
            if (charData.IsLineBreakCharData)
            {
                // 规定换行需要独立为一个 Run 哦
                if (GetImmutableRun() is { } run)
                {
                    yield return run;
                }

                yield return new LineBreakRun(charData.RunProperty);
            }
            else
            {
                if (lastChangedRunProperty is null)
                {
                    currentCharObjectList = new List<ICharObject>()
                    {
                        charData.CharObject
                    };
                    lastChangedRunProperty = charData.RunProperty;
                }
                else
                {
                    if (!charData.RunProperty.Equals(lastChangedRunProperty))
                    {
                        if (GetImmutableRun() is { } run)
                        {
                            yield return run;
                        }

                        currentCharObjectList = new List<ICharObject>()
                        {
                            charData.CharObject
                        };
                        lastChangedRunProperty = charData.RunProperty;
                    }
                    else
                    {
                        Debug.Assert(currentCharObjectList != null, nameof(currentCharObjectList) + " != null");
                        currentCharObjectList ??= new List<ICharObject>();
                        currentCharObjectList.Add(charData.CharObject);
                    }
                }
            }
        }

        if (GetImmutableRun() is { } immutableRun)
        {
            yield return immutableRun;
        }

        yield break;

        IImmutableRun? GetImmutableRun()
        {
            if (currentCharObjectList is not null && lastChangedRunProperty is not null)
            {
                return new CharObjectSpanTextRun(
                    new TextReadOnlyListSpan<ICharObject>(currentCharObjectList, 0, currentCharObjectList.Count),
                    lastChangedRunProperty);
            }

            currentCharObjectList = null;
            lastChangedRunProperty = null;
            return null;
        }
    }
}
