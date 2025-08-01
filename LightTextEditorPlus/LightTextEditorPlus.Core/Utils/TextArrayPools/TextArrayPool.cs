using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Utils.TextArrayPools;

internal static class TextArrayPool
{
    public static TextPoolArrayContext<T> Rent<T>(int minimumLength)
    {
        ArrayPool<T> arrayPool = ArrayPool<T>.Shared;
        T[] buffer = arrayPool.Rent(minimumLength);
        var textPoolArrayContext = new TextPoolArrayContext<T>(buffer, minimumLength, arrayPool);
        return textPoolArrayContext;
    }
}