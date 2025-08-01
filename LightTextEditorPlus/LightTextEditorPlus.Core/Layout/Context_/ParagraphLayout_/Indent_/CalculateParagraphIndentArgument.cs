using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout;

internal readonly record struct CalculateParagraphIndentArgument(ParagraphData CurrentParagraphData, ParagraphIndex ParagraphIndex, IReadOnlyList<ParagraphData> ParagraphList,
    UpdateLayoutContext UpdateLayoutContext);