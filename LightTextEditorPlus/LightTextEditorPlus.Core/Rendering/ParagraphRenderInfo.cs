using System.Collections;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 段落渲染信息
/// </summary>
public readonly struct ParagraphRenderInfo
{
    internal ParagraphRenderInfo(ParagraphIndex index, ParagraphData paragraphData, RenderInfoProvider renderInfoProvider)
    {
        Index = index;
        ParagraphData = paragraphData;
        _renderInfoProvider = renderInfoProvider;
    }

    /// <summary>
    /// 段落序号，这是文档里的第几段，从0开始
    /// </summary>
    public ParagraphIndex Index { get; }

    private readonly RenderInfoProvider _renderInfoProvider;

    /// <summary>
    /// 段落的布局数据
    /// </summary>
    public IParagraphLayoutData ParagraphLayoutData => ParagraphData.ParagraphLayoutData;

    /// <summary>
    /// 段落属性
    /// </summary>
    public ParagraphProperty ParagraphProperty => ParagraphData.ParagraphProperty;

    /// <summary>
    /// 段落
    /// </summary>
    public ITextParagraph Paragraph => ParagraphData;

    /// <summary>
    /// 段落
    /// </summary>
    internal ParagraphData ParagraphData { get; }

    /// <summary>
    /// 获取此段落内的行的渲染信息
    /// </summary>
    /// <returns></returns>
    public ParagraphLineRenderInfoList GetLineRenderInfoList()
    {
        return new ParagraphLineRenderInfoList(this);
    }

    #region 迭代器

    /// <summary>
    /// 提供段落内的行的渲染信息列表信息
    /// </summary>
    /// <param name="ParagraphRenderInfo"></param>
    public readonly record struct ParagraphLineRenderInfoList(ParagraphRenderInfo ParagraphRenderInfo) : IReadOnlyList<ParagraphLineRenderInfo>
    {
        internal ParagraphLineRenderInfo ToParagraphLineRenderInfo(int lineIndex)
        {
            LineLayoutData lineLayoutData = ParagraphRenderInfo.ParagraphData.LineLayoutDataList[lineIndex];

            var argument = lineLayoutData.GetLineDrawingArgument();

            ParagraphRenderInfo._renderInfoProvider.VerifyNotDirty();

            return new ParagraphLineRenderInfo(lineIndex: lineIndex, paragraphIndex: ParagraphRenderInfo.Index,
                argument, lineLayoutData, ParagraphRenderInfo.ParagraphData.ParagraphStartRunProperty,
                ParagraphRenderInfo._renderInfoProvider);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection
        /// </summary>
        /// <returns></returns>
        public ParagraphLineRenderInfoEnumerator GetEnumerator()
        {
            return new ParagraphLineRenderInfoEnumerator(this);
        }

        IEnumerator<ParagraphLineRenderInfo> IEnumerable<ParagraphLineRenderInfo>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => ParagraphRenderInfo.ParagraphData.LineLayoutDataList.Count;

        /// <inheritdoc />
        public ParagraphLineRenderInfo this[int index]
        {
            get => ToParagraphLineRenderInfo(index);
        }

        /// <summary>
        /// 枚举器，用于迭代段落内的行渲染信息
        /// </summary>
        /// <param name="List"></param>
        public record struct ParagraphLineRenderInfoEnumerator(ParagraphLineRenderInfoList List) : IEnumerator<ParagraphLineRenderInfo>
        {
            private int _index = -1;

            /// <inheritdoc />
            public void Dispose()
            {
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                _index++;
                if (_index >= List.Count)
                {
                    return false;
                }

                return true;
            }

            /// <inheritdoc />
            public void Reset()
            {
                _index = -1;
            }

            /// <inheritdoc />
            public ParagraphLineRenderInfo Current => List.ToParagraphLineRenderInfo(_index);
            object IEnumerator.Current => Current;
        }
    }

    #endregion
}