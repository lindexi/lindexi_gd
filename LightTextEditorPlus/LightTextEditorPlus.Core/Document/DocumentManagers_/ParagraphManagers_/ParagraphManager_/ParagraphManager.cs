using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Diagnostics.LogInfos;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落管理
/// </summary>
/// 段落的组织，段落的创建删除和查找
class ParagraphManager
{
    public ParagraphManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditorCore TextEditor { get; }

    /// <summary>
    /// 获取被指定的光标偏移命中的段落信息
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    public HitParagraphDataResult GetHitParagraphData(CaretOffset offset)
    {
        EnsureFirstParagraphExists();
        Debug.Assert(ParagraphList.Count != 0, "在确保首个段落存在之后，一定存在一段");

        if (offset.Offset == 0)
        {
            ParagraphData firstParagraph = ParagraphList[0];
            return GetResult(firstParagraph, hitOffset: null);
        }
        else
        {
            // 判断落在哪个段落里
            // 判断方法就是判断字符范围是否在段落内
            var currentDocumentOffset = 0;
            foreach (var paragraphData in ParagraphList)
            {
                var endOffset =
                    currentDocumentOffset + paragraphData.CharCount +
                    ParagraphData.DelimiterLength;

                // 设计：
                // 关于段末命中：
                // 如果 1\n2\n3 的文本情况。光标为 1|\n2\n3 时，行为就是首段的段末
                // 光标为 1\n|2\n3 时，行为就是第二段的段首，在 2 字符之前
                // 这个逻辑不受 offset.IsAtLineStart 的影响

                if (offset.Offset < endOffset)
                {
                    var hitParagraphOffset = offset.Offset - currentDocumentOffset;
                    // 当前的 DelimiterLength 设计是 \n 不存在加一问题
                    //if (hitParagraphOffset == paragraphData.CharCount + 1)
                    //{
                    //    // 命中到段末，自动修正
                    //    // 这里有加一问题
                    //    // 例如这一段是 12\n3
                    //    // 在传入命中 3 光标坐标时，命中到的是 \n 字符
                    //    hitParagraphOffset = paragraphData.CharCount;
                    //}

                    return GetResult(paragraphData, new ParagraphCaretOffset(hitParagraphOffset));
                }

                currentDocumentOffset = endOffset;
            }
        }

        // 没有落到哪个段落？那就抛个异常
        throw GetHitCaretOffsetOutOfRangeException();

        HitParagraphDataResult GetResult(ParagraphData paragraphData, ParagraphCaretOffset? hitOffset)
        {
            return new HitParagraphDataResult(offset, paragraphData,
                hitOffset ?? new ParagraphCaretOffset(0), this);
        }

        HitCaretOffsetOutOfRangeException GetHitCaretOffsetOutOfRangeException()
        {
            return new HitCaretOffsetOutOfRangeException(TextEditor, offset, TextEditor.DocumentManager.CharCount, nameof(offset));
        }
    }

    /// <summary>
    /// 获取最后一段。如果这个文本连一段都没有，那就创建新的一段
    /// </summary>
    /// <returns></returns>
    public ParagraphData GetLastParagraph()
    {
        EnsureFirstParagraphExists();
        // ReSharper disable once UseIndexFromEndExpression
        return ParagraphList[ParagraphList.Count - 1];
    }

    /// <summary>
    /// 创建段落且插入到某个段落后面
    /// </summary>
    /// <param name="relativeParagraph">相对的段落，如果是空，那将插入到最后</param>
    /// <param name="paragraphStartRunProperty">段落的字符起始属性</param>
    /// <returns></returns>
    public ParagraphData CreateParagraphAndInsertAfter(ParagraphData? relativeParagraph,
        IReadOnlyRunProperty? paragraphStartRunProperty = null)
    {
        ParagraphProperty? paragraphProperty = relativeParagraph?.ParagraphProperty;
        if (paragraphProperty == null)
        {
            // 不优化语法，方便加上断点

            // 获取当前的段落属性作为默认段落属性
            paragraphProperty = TextEditor.DocumentManager.StyleParagraphProperty;
        }

        paragraphProperty = paragraphProperty with
        {
            // 使用 with 关键词，重新拷贝一份对象，防止多个段落之间使用相同的段落对象属性，导致可能存在的对象变更
        };
        paragraphStartRunProperty ??=
            relativeParagraph?.ParagraphStartRunProperty ?? TextEditor.DocumentManager.StyleRunProperty;

        var paragraphData = new ParagraphData(paragraphStartRunProperty, paragraphProperty, this);

        if (relativeParagraph is null)
        {
            ParagraphList.Add(paragraphData);
        }
        else
        {
            var index = relativeParagraph.Index.Index;
            ParagraphList.Insert(index + 1, paragraphData);
        }

        return paragraphData;
    }

    public void RemoveParagraph(ParagraphData paragraphData)
    {
#if DEBUG
        paragraphData.IsDeleted = true;
#endif

        ParagraphList.Remove(paragraphData);
    }

    public void RemoveRange(int index, int count)
    {
#if DEBUG
        for (int i = index; i < count; i++)
        {
            ParagraphList[i].IsDeleted = true;
        }
#endif

        ParagraphList.RemoveRange(index, count);
    }

    /// <summary>
    /// 获取段落列表，获取时，至少会确保有一个段落存在
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<ParagraphData> GetParagraphList()
    {
        EnsureFirstParagraphExists();

        return ParagraphList;
    }

    /// <summary>
    /// 和 <see cref="GetParagraphList"/> 不同的是，这个方法返回的是原始的段落列表，不会确保至少有一个段落存在
    /// </summary>
    /// <returns></returns>
    internal IReadOnlyList<ParagraphData> GetRawParagraphList()
        => ParagraphList;

    public ParagraphData GetParagraph(ParagraphIndex index)
    {
        EnsureFirstParagraphExists();
        var list = ParagraphList;
        if (index.Index >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index),
                $"ParagraphCount:{list.Count} Index={index}");
        }

        return list[index.Index];
    }

    private List<ParagraphData> ParagraphList { get; } = new List<ParagraphData>();

    //public void InsertParagraphAfter(ParagraphData currentParagraph, ParagraphData newParagraph)
    //{
    //    var index = ParagraphList.IndexOf(currentParagraph);
    //    ParagraphList.Insert(index + 1, newParagraph);
    //}

    /// <summary>
    /// 获取文本行的起始位置在文档中的偏移量，此偏移量的计算考虑了换行符，如123/r/n123，那么第二个段落的Offset为5
    /// </summary>
    /// <exception cref="InvalidOperationException">这个文本行被删除后引发此异常</exception>
    public DocumentOffset GetParagraphStartOffset(ParagraphData paragraphData)
    {
        var offset = 0;
        foreach (var current in ParagraphList)
        {
            if (ReferenceEquals(current, paragraphData))
            {
                return new DocumentOffset(offset);
            }
            else
            {
                offset += current.CharCount + ParagraphData.DelimiterLength;
            }
        }

        // 没有找到段落，证明段落被删除
        throw new InvalidOperationException();
    }

    public ParagraphIndex GetParagraphIndex(ParagraphData paragraphData)
    {
        var index = ParagraphList.IndexOf(paragraphData);
        return new ParagraphIndex(index);
    }

    /// <summary>
    /// 确保首个段落存在
    /// </summary>
    /// 即使是一个空文本，设计上也是存在一个空段落的
    private void EnsureFirstParagraphExists()
    {
        if (ParagraphList.Count == 0)
        {
            TextEditor.Logger.Log(new EnsureFirstParagraphExistsLogInfo());
            CreateParagraphAndInsertAfter(relativeParagraph: null);
        }
    }
}
