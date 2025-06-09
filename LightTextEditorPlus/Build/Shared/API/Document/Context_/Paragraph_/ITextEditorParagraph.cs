#if TopApiTextEditorDefinition
using System.Collections.Generic;

using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 表示文本的一个段落
/// </summary>
public interface ITextEditorParagraph : ITextParagraph
{
    /// <summary>
    /// 获取段落的字符数据列表
    /// </summary>
    /// <returns></returns>
    new IReadOnlyList<CharInfo> GetParagraphCharDataList();
}

#endif
