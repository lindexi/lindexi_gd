using System.Runtime.CompilerServices;
using System.Text;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public sealed unsafe class ImeCompositionContextReader
{
    private readonly IImmContextAccessor _contextAccessor;

    public ImeCompositionContextReader(IImmContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public bool IsComposing(HImc inputContext)
    {
        if (inputContext.Value == 0)
        {
            return false;
        }

        var inputContextPointer = _contextAccessor.LockInputContext(inputContext);
        if (inputContextPointer == 0)
        {
            return false;
        }

        try
        {
            var context = (InputContext*)inputContextPointer;
            if (context->HCompStr == 0)
            {
                return false;
            }

            var compositionPointer = _contextAccessor.LockCompositionString(context->HCompStr);
            if (compositionPointer == 0)
            {
                return false;
            }

            try
            {
                return IsCompositionStringActive((CompositionString*)compositionPointer);
            }
            finally
            {
                _contextAccessor.UnlockCompositionString(context->HCompStr);
            }
        }
        finally
        {
            _contextAccessor.UnlockInputContext(inputContext);
        }
    }

    public bool IsCompositionStringActiveForTesting(CompositionString* compositionString)
    {
        return IsCompositionStringActive(compositionString);
    }

    private static bool IsCompositionStringActive(CompositionString* compositionString)
    {
        if (compositionString is null || compositionString->Size < Unsafe.SizeOf<CompositionString>())
        {
            return false;
        }

        if (compositionString->CompStrLength == 0)
        {
            return false;
        }

        if (!IsValidRange(compositionString->Size, compositionString->CompStrOffset, compositionString->CompStrLength))
        {
            return false;
        }

        var compositionBytes = new ReadOnlySpan<byte>((byte*)compositionString + compositionString->CompStrOffset, (int)compositionString->CompStrLength);
        return !string.IsNullOrEmpty(Encoding.Unicode.GetString(compositionBytes));
    }

    private static bool IsValidRange(uint size, uint offset, uint length)
    {
        return offset >= Unsafe.SizeOf<CompositionString>()
            && offset <= size
            && length <= size - offset;
    }
}