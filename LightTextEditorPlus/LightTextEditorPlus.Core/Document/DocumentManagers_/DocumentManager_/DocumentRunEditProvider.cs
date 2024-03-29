﻿using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文档的字符编辑提供器。不对外，只处理字符编辑
/// </summary>
/// 为了让 <see cref="DocumentManager"/> 不要包含太多的逻辑，就将编辑字符相关的独立在这个类型
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
        // 1. 如果当前段落是空，那么追加时，继承当前段落的字符属性样式
        // 2. 如果当前段落已有文本，那么追加时，使用此段落最后一个字符的字符属性作为字符属性样式
        IReadOnlyRunProperty styleRunProperty;
        var index = lastParagraph.CharCount - 1;
        if (index < 0)
        {
            // 如果当前段落是空，那么追加时，继承当前段落的字符属性样式
            styleRunProperty = lastParagraph.ParagraphProperty.ParagraphStartRunProperty ??
                               TextEditor.DocumentManager.CurrentRunProperty;
        }
        else
        {
            // 如果当前段落已有文本，那么追加时，使用此段落最后一个字符的字符属性作为字符属性样式
            var charData = lastParagraph.GetCharData(new ParagraphCharOffset(index));
            styleRunProperty = charData.RunProperty;
        }

        AppendRunToParagraph(run, lastParagraph, styleRunProperty);
    }

    public void Replace(Selection selection, IImmutableRun run)
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

        InsertInner(selection.StartOffset, run, styleRunProperty);
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
                // 如果这是一个分段，那直接插入新的段落
                var newParagraph = ParagraphManager.CreateParagraphAndInsertAfter(currentParagraph);

                // todo 如果有明确的分行，那就给定一个段落的字符属性
                //newParagraph.ParagraphProperty.ParagraphStartRunProperty = runProperty;

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
        // todo 实现删除逻辑
    }
}