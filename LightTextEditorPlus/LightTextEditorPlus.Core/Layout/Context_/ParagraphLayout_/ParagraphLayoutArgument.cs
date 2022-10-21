using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

readonly record struct ParagraphLayoutArgument(int ParagraphIndex, Point CurrentStartPoint, ParagraphData ParagraphData, IReadOnlyList<ParagraphData> ParagraphList)
{
}