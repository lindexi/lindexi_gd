using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Utils;

internal static class TextEditorInnerDebugAsset
{
    public static void Assert(bool condition, string message)
    {
        if (condition is false)
        {
            throw new TextEditorInnerDebugException(message);
        }
    }

    public static void AreEquals(double expect, double actual, string name)
    {
        if (Nearly.Equals(expect, actual) is false)
        {
            throw new TextEditorInnerDebugException($"对 {name} 的预期和实际值不符。预期：{expect}，实际：{actual}");
        }
    }
}
