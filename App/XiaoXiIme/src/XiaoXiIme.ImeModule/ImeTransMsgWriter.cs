using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static unsafe class ImeTransMsgWriter
{
    public static uint Write(nint transKey, ReadOnlySpan<TransMsg> messages)
    {
        if (transKey == 0 || messages.IsEmpty)
        {
            return 0;
        }

        var list = (TransMsgList*)transKey;
        list->Count = (uint)messages.Length;

        var target = &list->Message;
        for (var i = 0; i < messages.Length; i++)
        {
            target[i] = messages[i];
        }

        return (uint)messages.Length;
    }
}
