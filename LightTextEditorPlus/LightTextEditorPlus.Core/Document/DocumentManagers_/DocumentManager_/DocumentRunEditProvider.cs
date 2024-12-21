using System;
using System.Collections.Generic;
using System.Diagnostics;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文档的字符编辑提供器。不对外，只处理字符编辑
/// </summary>
/// 为了让 <see cref="DocumentManager"/> 不要包含太多的逻辑，就将编辑字符相关的独立在这个类型
/// 和 <see cref="ParagraphManager"/> 的不同在于，只让 <see cref="ParagraphManager"/> 处理存放段落的功能，不需要关心字符编辑的功能。字符编辑为了让代码比较方便调用，需要许多辅助方法。这些辅助方法将会让总体逻辑复杂，于是放在独立的类型，简化框架
internal class DocumentRunEditProvider
{
    public DocumentRunEditProvider(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public ParagraphManager ParagraphManager => TextEditor.DocumentManager.ParagraphManager;
    public TextEditorCore TextEditor { get; }

    public void Append(IImmutableRun run)
    {
        // 追加是使用最多的，需要做额外的优化
        var lastParagraph = ParagraphManager.GetLastParagraph();

        // 追加的样式继承规则：
        // 1. 当前光标样式存在，则采用当前光标样式，否则执行以下判断
        // 2. 如果当前段落是空，那么追加时，继承当前段落的字符属性样式
        // 3. 如果当前段落已有文本，那么追加时，使用此段落最后一个字符的字符属性作为字符属性样式
        // 以上规则就是 DocumentManager.CurrentCaretRunProperty 属性的值
        IReadOnlyRunProperty styleRunProperty = TextEditor.DocumentManager.CurrentCaretRunProperty;

        AppendRunToParagraph(run, lastParagraph, styleRunProperty);
    }

    public void Replace(in Selection selection, IImmutableRunList? runList)
    {
        // 替换的时候，需要处理文本的字符属性样式
        IReadOnlyRunProperty? styleRunProperty = null;
        // 规则：
        // 1. 替换文本时，采用靠近文档的光标的后续一个字符的字符属性
        // 2. 仅加入新文本时，采用光标的前一个字符的字符属性

        // 先执行删除，再执行插入
        if (selection.Length != 0)
        {
            var frontOffset = selection.FrontOffset;

            var paragraphDataResult = ParagraphManager.GetHitParagraphData(frontOffset);

            if (runList is not null)
            {
                /*
                 * 替换文本时，采用靠近文档的光标的后续一个字符的字符属性
                 * 0 1 2 3 ------ 光标偏移量
                   | | | |
                    A B C  ------ 字符 
                 * 假设光标是 1 的值，那将取 B 字符，因此换算方法就是获取当前光标的偏移转换
                 */
                if (paragraphDataResult.ParagraphData.CharCount == 0)
                {
                    // 这是一个空段，那就采用段落属性
                    styleRunProperty = paragraphDataResult.ParagraphData.ParagraphProperty.ParagraphStartRunProperty;
                }
                else
                {
                    var paragraphCharOffset = new ParagraphCharOffset(paragraphDataResult.HitOffset.Offset);
                    var charData = paragraphDataResult.ParagraphData.GetCharData(paragraphCharOffset);
                    styleRunProperty = charData.RunProperty;
                }
            }
            else
            {
                // 没有任何需要加入的内容，也就不需要 styleRunProperty 的值
            }

            RemoveInner(selection, paragraphDataResult);
        }
        else
        {
            // 没有替换的长度，加入即可
        }

        if (runList is not null)
        {
            InsertInner(selection.FrontOffset, runList, styleRunProperty);
        }
        else
        {
            // 没有任何需要加入的内容，那也就是删除
        }
    }

    /// <summary>
    /// 在文档指定位移<paramref name="offset"/>处插入一段文本
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="runList"></param>
    /// <param name="styleRunProperty">继承的样式，如果非替换，仅加入，那这是空</param>
    private void InsertInner(CaretOffset offset, IImmutableRunList runList, IReadOnlyRunProperty? styleRunProperty)
    {
        // 插入的逻辑，找到插入变更的行
        var paragraphDataResult = ParagraphManager.GetHitParagraphData(offset);
        var paragraphData = paragraphDataResult.ParagraphData;

        if (styleRunProperty is null)
        {
            // 仅加入 styleRunProperty 是空
            // 仅加入新文本时，如果是在当前光标下插入的，先获取当前光标样式
            // 仅加入新文本时，采用光标的前一个字符的字符属性
            /*
             * 仅加入新文本时，采用光标的前一个字符的字符属性
             * 0 1 2 3 ------ 光标偏移量
               | | | |
                A B C  ------ 字符 
             * 假设光标是 1 的值，那将取 A 字符，因此换算方法就是获取当前光标的前面一个字符
             */
            if (offset == TextEditor.CaretManager.CurrentCaretOffset)
            {
                // 由于 TextEditor.DocumentManager.CurrentCaretRunProperty 需要执行一次命中测试，存在重复计算。为了减少重复计算，这里使用更底层的 CaretManager.CurrentCaretRunProperty 属性
                // 如果用户没有设置当前光标的样式，那依靠后续逻辑找到样式
                styleRunProperty = TextEditor.CaretManager.CurrentCaretRunProperty;
            }

            if (styleRunProperty is null)
            {
                if (paragraphDataResult.HitOffset.Offset == 0)
                {
                    // 规定，光标是 0 获取段落的字符属性
                    styleRunProperty = paragraphData.ParagraphProperty.ParagraphStartRunProperty;
                }
                else
                {
                    var charData = paragraphDataResult.GetHitCharData();
                    styleRunProperty = charData!.RunProperty;
                }
            }
        }

        // 看看是不是在段落中间插入的，如果在段落中间插入的，需要将段落中间移除掉
        // 在插入完成之后，重新加入
        var lastParagraphRunList = paragraphData.SplitRemoveByParagraphOffset(paragraphDataResult.HitOffset);

        // 追加文本，获取追加需要加入的当前段落
        var currentParagraph = paragraphData;

        var runCount = runList.RunCount;
        for (int i = 0; i < runCount; i++)
        {
            var run = runList.GetRun(i);
            currentParagraph = AppendRunToParagraph(run, currentParagraph, styleRunProperty);
        }

        if (lastParagraphRunList != null)
        {
            // 如果是从一段的中间插入的，需要将这一段在插入点后面的内容继续放入到当前的段落
            currentParagraph.AppendCharData(lastParagraphRunList);
        }
    }

    /// <summary>
    /// 给段落追加文本
    /// </summary>
    /// <param name="run"></param>
    /// <param name="paragraphData"></param>
    /// <param name="styleRunProperty">如果 <paramref name="run"/> 没有字符属性，将继承使用这个属性</param>
    /// <returns>由于文本追加可能带上换行符，会新加段落。返回当前的段落</returns>
    private ParagraphData AppendRunToParagraph(IImmutableRun run, ParagraphData paragraphData,
        IReadOnlyRunProperty styleRunProperty)
    {
        var runProperty = run.RunProperty ?? styleRunProperty;

        // 当前的段落，如果插入的分段的内容，那自然需要自动分段
        var currentParagraph = paragraphData;
        // 看起来不需要中间插入逻辑，只需要插入到最后
        //var insertOffset = offset;

        // 获取 run 的分段逻辑，大部分情况下都是按照 \r\n 作为分段逻辑
        var runParagraphSplitter = TextEditor.PlatformProvider.GetRunParagraphSplitter();
        bool isFirstSubRun = true;
        foreach (var subRun in runParagraphSplitter.Split(run))
        {
            if (subRun is LineBreakRun || !isFirstSubRun)
            {
                // 如果有明确的分段，那就给定一个段落的字符属性
                var paragraphStartRunProperty = runProperty;

                // 如果这是一个分段，那直接插入新的段落
                var newParagraph =
                    ParagraphManager.CreateParagraphAndInsertAfter(currentParagraph, paragraphStartRunProperty);

                // todo 设置最后一行是脏的
                // 当前是设置整个段落是脏的
                currentParagraph.SetDirty();

                currentParagraph = newParagraph;

                //insertOffset = ParagraphManager.GetParagraphStartDocumentOffset(currentParagraph);
            }
            else
            {
                //paragraphData.InsertRun(insertOffset,subRun);
                //insertOffset += subRun.Count;
                isFirstSubRun = false;
            }

            currentParagraph.AppendRun(subRun, runProperty);
        }

        return currentParagraph;
    }

    /// <summary>
    /// 删除一段文本
    /// </summary>
    /// <param name="selection"></param>
    /// <param name="paragraphDataResult"></param>
    /// 这里属于核心实现方法，暂时不优化方法代码长度。因为拆分多个方法之后，反而不方便调试，且我也没有找到拆分之后更加清晰的写法
    private void RemoveInner(in Selection selection, in HitParagraphDataResult paragraphDataResult)
    {
        if (selection.IsEmpty)
        {
            // 没有删除内容，那就啥都不做
            return;
        }

        // 如果删除的内容小于段落长度，那就在当前段落内完成
        var firstParagraph = paragraphDataResult.ParagraphData;
        // 先找到插入点是哪一段
        ParagraphCaretOffset hitOffset = paragraphDataResult.HitOffset; // 这里可是段落内坐标哦
        // 剩余可以删除的长度
        var remainLength = selection.Length;
        // 段落开始删除的点
        var paragraphStartOffset = hitOffset;
        var paragraphData = firstParagraph;
        var paragraphManager = paragraphDataResult.ParagraphManager;
        var paragraphList = paragraphManager.GetParagraphList();

        while (remainLength > 0)
        {
            // 这一段可以删除的长度
            var removeLength = Math.Min(remainLength, paragraphData.CharCount - paragraphStartOffset.Offset);

            if (removeLength == 0)
            {
                // 这一段没有什么可以删除的，且依然存在需要删除的长度，那就是删除空段了
                if (paragraphData.CharCount == paragraphStartOffset.Offset)
                {
                    // 进入下一段，继续删除
                    ToNextParagraph();
                    continue;
                }
                else
                {
                    // 不知道是啥情况，理论上不会存在
                    throw new TextEditorInnerException($"一段里面没有任何可删除的字符，但是光标不在段末");
                }
            }

            // 如果是在一段的中间删除，那就删除段落中间，需要先拿到段落删除中间之后的后面字符
            var paragraphEndOffset = new ParagraphCaretOffset(paragraphStartOffset.Offset + removeLength);
            IList<CharData>? lastCharDataList = null;
            if (paragraphData.CharCount > paragraphEndOffset.Offset)
            {
                // 如果不是删除到全段，那就需要将段落删除之后剩下的部分重新加入
                lastCharDataList = paragraphData.SplitRemoveByParagraphOffset(paragraphEndOffset);

                Debug.Assert(remainLength == removeLength, "如果一段删除之后还有剩余的，那就是完全删除，删除的长度就和需要删除的长度相同");
            }

            // 然后再从段落开始删除时开始删除
            paragraphData.RemoveRange(paragraphStartOffset);

            // 如果删除不全段，那就将段落之后的加回，尽管最后段落可能还会被删除，存在重复的加入情况
            // 例如 1 和 2 两个段落，删除是从 1 段删除到 2 段，且 2 段没有完全删除
            // 以下逻辑就是将 2 段的剩余部分，先加入到 2 段里面。最后再删除 2 段，将 2 段所有内容加入回 1 段里面。也就是 lastCharDataList 最差情况下，需要被加入到 2 段再从 2 段取出加入到 1 段里面
            if (lastCharDataList is not null)
            {
                paragraphData.AppendCharData(lastCharDataList);
            }

            ToNextParagraph();

            void ToNextParagraph()
            {
                remainLength -= removeLength;

                if (remainLength <= 0)
                {
                    // 如果减去当前段落之后，完成删除，那就不需要减去下一段了
                    return;
                }

                // 如果这一段没有足够减去的，那就继续减去下一段的

                // 这一段不够删除了，下到下一段
                if (paragraphData.Index == paragraphList.Count - 1)
                {
                    // 最后一段，理论上不会存在
                    throw new TextEditorInnerException($"删除文本时，超过文本框所能提供的字符范围");
                }
                else
                {
                    // 取下一段
                    var nextParagraphDataIndex = paragraphData.Index + 1;
                    var nextParagraphData = paragraphList[nextParagraphDataIndex];

                    paragraphData = nextParagraphData;
                    // 取下一段就是从头开始删除了
                    paragraphStartOffset = new ParagraphCaretOffset(0);
                    // 减去换行符
                    remainLength -= ParagraphData.DelimiterLength;
                }
            }
        }

        if (ReferenceEquals(firstParagraph, paragraphData))
        {
            // 如果当前段落和首个开始删除的段落相同，那就证明没有删除任何一段
            // 否则就需要执行合并
            firstParagraph.SetDirty();
        }
        else
        {
            var start = firstParagraph.Index;
            var end = paragraphData.Index;

            // 如果最后一段不是完全删除的
            // 只需要加上最后一段的即可，中间的都是被全部删除的
            var lastParagraphCharDataList = paragraphData.ToReadOnlyListSpan(new ParagraphCharOffset(0));
            foreach (var charData in lastParagraphCharDataList)
            {
                charData.CharLayoutData = null;
                firstParagraph.AppendCharData(charData);
            }

            firstParagraph.SetDirty();

            paragraphManager.RemoveRange(start + 1, end - start);
        }
    }
}
