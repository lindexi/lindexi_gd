using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document.DocumentManagers;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Document;

internal class TextRunManager
{
    public TextRunManager(TextEditorCore textEditor)
    {
        ParagraphManager = new ParagraphManager(textEditor);
    }

    public ParagraphManager ParagraphManager { get; }

    public void Replace(SelectionSegment selection, IRun run)
    {
        // 先执行删除，再执行插入
        if (selection.SectionLength != 0)
        {
            RemoveInner(selection.SelectionStart,selection.SectionLength);
        }
        else
        {
            // 没有替换的长度，加入即可
        }

        InsertInner(selection.SelectionStart, run);
    }

    /// <summary>
    /// 在文档指定位移<paramref name="offset"/>处插入一段文本
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="run"></param>
    private void InsertInner(int offset, IRun run)
    {
        // 插入的逻辑，找到插入变更的行
        var paragraphData = ParagraphManager.GetParagraphData(offset);

        // 获取 run 的分段逻辑，大部分情况下都是按照 \r\n 作为分段逻辑
    }

    private void RemoveInner(int offset, int length)
    {
        // todo 实现删除逻辑
    }
}

/// <summary>
/// 段落管理
/// </summary>
class ParagraphManager
{
    public ParagraphManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditorCore TextEditor { get; }

    public ParagraphData GetParagraphData(DocumentOffset offset)
    {
        if (ParagraphList.Count == 0)
        {
            // 获取当前的段落属性作为默认段落属性
            var paragraphProperty = TextEditor.DocumentManager.CurrentParagraphProperty;

            var paragraphData = new ParagraphData(paragraphProperty);
            ParagraphList.Add(paragraphData);
            return paragraphData;
        }
        else
        {
            //todo 还没实现非空行的逻辑
            throw new NotImplementedException();
        }
    }

    public List<ParagraphData> ParagraphList { get; } = new List<ParagraphData>();
}

/// <summary>
/// 段落数据
/// </summary>
class ParagraphData
{
    public ParagraphData(ParagraphProperty paragraphProperty)
    {
        ParagraphProperty = paragraphProperty;
    }

    public ParagraphProperty ParagraphProperty { set; get; }

    public List<ITextRun> TextRunList { get; } = new List<ITextRun>();

    /// <summary>
    /// 这一行的字符长度
    /// </summary>
    public int CharCount { get; }

    /// <summary>
    /// 获取这个文本行是否已经从文档中删除.
    /// </summary>
    public bool IsDeleted { set; get; }
}