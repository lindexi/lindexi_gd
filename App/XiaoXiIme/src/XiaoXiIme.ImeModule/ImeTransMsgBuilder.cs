using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

public static class ImeTransMsgBuilder
{
    public static TransMsg[] BuildMessages(ImeToAsciiResult result, HWnd hwnd = default)
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
                    Hwnd = hwnd,
                    Message = ImeConstants.WmImeComposition,
                    WParam = result.CommitText[0],
                    LParam = (nint)ImeConstants.GcsResultStr,
                },
                new TransMsg
                {
                    Hwnd = hwnd,
                    Message = ImeConstants.WmImeEndComposition,
                },
            ];
        }

        if (result.Snapshot.IsComposing)
        {
            return
            [
                new TransMsg
                {
                    Hwnd = hwnd,
                    Message = ImeConstants.WmImeStartComposition,
                },
                new TransMsg
                {
                    Hwnd = hwnd,
                    Message = ImeConstants.WmImeComposition,
                    LParam = (nint)(ImeConstants.GcsCompStr
                        | ImeConstants.GcsCompReadStr
                        | ImeConstants.GcsCursorPos
                        | ImeConstants.GcsCandidateInfo
                        | ImeConstants.GcsGuideLine
                        | ImeConstants.GcsPrivate),
                },
            ];
        }

        return
        [
            new TransMsg
            {
                Hwnd = hwnd,
                Message = ImeConstants.WmImeEndComposition,
            },
        ];
    }
}
