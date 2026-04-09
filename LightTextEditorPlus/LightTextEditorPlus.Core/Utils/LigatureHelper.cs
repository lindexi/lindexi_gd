using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Utils;

internal static class LigatureHelper
{
    public static CharData FindLigatureStartCharData(in TextReadOnlyListSpan<CharData> lineCharDataList, int offset)
    {
        var firstCharData = lineCharDataList[offset];
        Debug.Assert(firstCharData.CharDataInfo.Status == CharDataInfoStatus.LigatureContinue);

        for (int i = offset - 1; i >= 0; i--)
        {
            var currentCharData = lineCharDataList[i];
            if (currentCharData.CharDataInfo.Status == CharDataInfoStatus.LigatureStart)
            {
                return currentCharData;
            }
        }

        throw new TextEditorInnerException(
            ExceptionMessages.Get(nameof(LigatureHelper) + "_CannotFindLigatureStart"));
    }
}
