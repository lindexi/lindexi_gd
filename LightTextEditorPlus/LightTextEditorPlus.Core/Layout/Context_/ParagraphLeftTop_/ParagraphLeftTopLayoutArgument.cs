using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

readonly record struct ParagraphLeftTopLayoutArgument(int ParagraphIndex, Point CurrentLeftTop, ParagraphData ParagraphData, IReadOnlyList<ParagraphData> ParagraphList)
{
}