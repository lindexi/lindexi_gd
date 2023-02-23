using System;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;

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
    private CaretManager CaretManager => TextEditor.CaretManager;

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

    public void Replace(in Selection selection, IImmutableRun? run)
    {
        // 替换的时候，需要处理文本的字符属性样式
        IReadOnlyRunProperty? styleRunProperty = null;
        // 规则：
        // 1. 替换文本时，采用靠近文档的光标的后续一个字符的字符属性
        // 2. 仅加入新文本时，采用光标的前一个字符的字符属性

        // 先执行删除，再执行插入
        if (selection.Length != 0)
        {
            var paragraphDataResult = ParagraphManager.GetHitParagraphData(selection.StartOffset);

            /*
             * 替换文本时，采用靠近文档的光标的后续一个字符的字符属性
             * 0 1 2 3 ------ 光标偏移量
               | | | |
                A B C  ------ 字符 
             * 假设光标是 1 的值，那将取 B 字符，因此换算方法就是获取当前光标的偏移转换
             */
            var paragraphCharOffset = new ParagraphCharOffset(paragraphDataResult.HitOffset.Offset);
            var charData = paragraphDataResult.ParagraphData.GetCharData(paragraphCharOffset);
            styleRunProperty = charData.RunProperty;

            RemoveInner(selection);
        }
        else
        {
            // 没有替换的长度，加入即可
        }

        if (run is not null)
        {
            InsertInner(selection.StartOffset, run, styleRunProperty);
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
    /// <param name="run"></param>
    /// <param name="styleRunProperty">继承的样式，如果非替换，仅加入，那这是空</param>
    private void InsertInner(CaretOffset offset, IImmutableRun run, IReadOnlyRunProperty? styleRunProperty)
    {
        // 插入的逻辑，找到插入变更的行
        var paragraphDataResult = ParagraphManager.GetHitParagraphData(offset);
        var paragraphData = paragraphDataResult.ParagraphData;

        if (styleRunProperty is null)
        {
            // 仅加入 styleRunProperty 是空
            // 仅加入新文本时，采用光标的前一个字符的字符属性
            /*
             * 仅加入新文本时，采用光标的前一个字符的字符属性
             * 0 1 2 3 ------ 光标偏移量
               | | | |
                A B C  ------ 字符 
             * 假设光标是 1 的值，那将取 A 字符，因此换算方法就是获取当前光标的前面一个字符
             */
            if (paragraphDataResult.HitOffset.Offset == 0)
            {
                // 规定，光标是 0 获取段落的字符属性
                styleRunProperty = paragraphData.ParagraphProperty.ParagraphStartRunProperty ??
                                   TextEditor.DocumentManager.CurrentRunProperty;
            }
            else
            {
                var paragraphCharOffset = new ParagraphCharOffset(paragraphDataResult.HitOffset.Offset - 1);
                var charData = paragraphData.GetCharData(paragraphCharOffset);
                styleRunProperty = charData.RunProperty;
            }
        }

        // 看看是不是在段落中间插入的，如果在段落中间插入的，需要将段落中间移除掉
        // 在插入完成之后，重新加入
        var lastParagraphRunList = paragraphData.SplitRemoveByParagraphOffset(paragraphDataResult.HitOffset);

        // 追加文本，获取追加之后的当前段落
        var currentParagraph = AppendRunToParagraph(run, paragraphData, styleRunProperty);

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

        // 当前的段落，如果插入的分行的内容，那自然需要自动分段
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
                // 如果有明确的分行，那就给定一个段落的字符属性
                var paragraphStartRunProperty = runProperty;

                // 如果这是一个分段，那直接插入新的段落
                var newParagraph = ParagraphManager.CreateParagraphAndInsertAfter(currentParagraph, paragraphStartRunProperty);

                currentParagraph = newParagraph;

                //insertOffset = ParagraphManager.GetParagraphStartDocumentOffset(currentParagraph);
            }
            else
            {
                //paragraphData.InsertRun(insertOffset,subRun);
                //insertOffset += subRun.Count;
            }

            currentParagraph.AppendRun(subRun, runProperty);

            isFirstSubRun = false;
        }

        return currentParagraph;
    }

    private void RemoveInner(Selection selection)
    {
        if (selection.IsEmpty)
        {
            // 没有删除内容，那就啥都不做
            return;
        }

        // 先找到插入点是哪一段
        var frontOffset = selection.FrontOffset;
        var paragraphDataResult = ParagraphManager.GetHitParagraphData(frontOffset);

        // 如果删除的内容小于段落长度，那就在当前段落内完成
        var paragraphData = paragraphDataResult.ParagraphData;
        ParagraphCaretOffset hitOffset = paragraphDataResult.HitOffset; // 这里可是段落内坐标哦
        var remainLength = selection.Length;
        var paragraphStartOffset = hitOffset;

        // 这一段可以删除的长度
        var removeLength = Math.Min(remainLength, paragraphData.CharCount - paragraphStartOffset.Offset);

        // 如果是在一段的中间删除，那就删除段落中间，需要先拿到段落删除中间之后的后面字符
        var paragraphEndOffset = new ParagraphCaretOffset(paragraphStartOffset.Offset + removeLength);
        IList<CharData>? lastCharDataList = null;
        if (paragraphData.CharCount > paragraphEndOffset.Offset)
        {
            // 如果不是删除到全段
            lastCharDataList = paragraphData.SplitRemoveByParagraphOffset(paragraphEndOffset);
        }
        // 然后再从段落开始删除时开始删除
        var deletedCharDataList = paragraphData.SplitRemoveByParagraphOffset(paragraphStartOffset);

        // 这个 deletedCharDataList 就是被删除的字符
        // 下面这个代码只是让 VS 了解到这个变量是用来调试的
        _ = deletedCharDataList;

        // 如果删除不全段，那就将段落之后的加回
        if (lastCharDataList is not null)
        {
            paragraphData.AppendCharData(lastCharDataList);
        }
    }
}