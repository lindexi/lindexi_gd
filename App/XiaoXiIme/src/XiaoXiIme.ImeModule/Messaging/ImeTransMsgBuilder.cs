using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static class ImeTransMsgBuilder
{
    public static TransMsg[] BuildMessages(ImeToAsciiResult result) => BuildMessages(result, wasComposing: false);

    internal static TransMsg[] BuildMessages(ImeToAsciiResult result, bool wasComposing)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.Handled)
        {
            return [];
        }

        if (!string.IsNullOrEmpty(result.CommitText))
        {
            return
            [
                new TransMsg
                {
                    Message = ImeConstants.WmImeComposition,
                    WParam = result.CommitText[0],
                    LParam = (nint)ImeConstants.GcsResultStr,
                },
                new TransMsg
                {
                    Message = ImeConstants.WmImeEndComposition,
                },
            ];
        }

        if (result.Snapshot.IsComposing)
        {
            TransMsg[] messages =
            [
                new TransMsg
                {
                    Message = ImeConstants.WmImeStartComposition,
                },
                new TransMsg
                {
                    Message = ImeConstants.WmImeComposition,
                    LParam = (nint)(ImeConstants.GcsCompStr
                                    | ImeConstants.GcsCompReadStr
                                    | ImeConstants.GcsCursorPos
                                    | ImeConstants.GcsCandidateInfo
                                    | ImeConstants.GcsGuideLine
                                    | ImeConstants.GcsPrivate),
                },
            ];
            return wasComposing ? messages[1..] : messages;
        }

        if (!wasComposing)
        {
            return [];
        }

        return
        [
            new TransMsg
            {
                Message = ImeConstants.WmImeEndComposition,
            },
        ];
    }
}