using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static class ImeTransMsgBuilder
{
    public static TransMsg[] BuildMessages(ImeToAsciiResult result) => BuildMessages(result, wasComposing: false);

    internal static TransMsg[] BuildMessages
        (ImeToAsciiResult result, bool wasComposing, bool includeLegacyFlags = true)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.Handled)
        {
            return [];
        }

        if (result.CommitText is { Length: > 0 } commit && result.Snapshot.IsComposing)
        {
            return [new TransMsg
            {
                Message = ImeConstants.WmImeComposition,
                WParam = commit[0],
                LParam = (nint)(ImeConstants.GcsResultStr | ImeConstants.GcsCompStr | ImeConstants.GcsCompReadStr | ImeConstants.GcsCursorPos),
            }];
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
                                    | (includeLegacyFlags
                                        ? ImeConstants.GcsCandidateInfo | ImeConstants.GcsGuideLine | ImeConstants.GcsPrivate
                                        : 0u)),
                },
            ];
            return wasComposing ? messages[1..] : messages;
        }

        if (!wasComposing)
        {
            return [];
        }

        // Inline clients need the empty composition update to remove preedit; END alone does not clear it.
        return includeLegacyFlags
            ? [new TransMsg { Message = ImeConstants.WmImeEndComposition }]
            :
            [
                new TransMsg { Message = ImeConstants.WmImeComposition },
                new TransMsg { Message = ImeConstants.WmImeEndComposition },
            ];
    }
}