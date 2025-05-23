using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Utils.Maths;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Utils;

internal static class TextEditorInnerDebugAsset
{
    public static void Equals(double expect, double actual, string name)
    {
        if (Nearly.Equals(expect, actual) is false)
        {
            throw new TextEditorInnerDebugException($"对 {name} 的预期和实际值不符。预期：{expect}，实际：{actual}");
        }
    }
}
