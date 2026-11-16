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

    internal IImmContextAccessor ContextAccessor => _contextAccessor;

    /// <summary>Checks the target IMM context rather than another session's conversion mode.</summary>
    public bool IsReceivingChinese(HImc inputContext)
    {
        if (inputContext.Value == 0) return true;
        var pointer = _contextAccessor.LockInputContext(inputContext);
        if (pointer == 0) return false;
        try
        {
            var context = (InputContext*)pointer;
            return context->FOpen != 0 &&
                (context->FdwConversion & ImeConstants.ImeCmodeNative) != 0 &&
                (context->FdwConversion & ImeConstants.ImeCmodeNoConversion) == 0;
        }
        finally
        {
            _contextAccessor.UnlockInputContext(inputContext);
        }
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

        if (compositionString->CompStrLength > int.MaxValue / sizeof(char))
        {
            return false;
        }

        var byteLength = compositionString->CompStrLength * sizeof(char);
        if (!IsValidRange(compositionString->Size, compositionString->CompStrOffset, byteLength))
        {
            return false;
        }

        var compositionBytes = new ReadOnlySpan<byte>((byte*)compositionString + compositionString->CompStrOffset, (int)byteLength);
        return !string.IsNullOrEmpty(Encoding.Unicode.GetString(compositionBytes));
    }

    private static bool IsValidRange(uint size, uint offset, uint length)
    {
        return offset >= Unsafe.SizeOf<CompositionString>()
            && offset <= size
            && length <= size - offset;
    }
}