using System.Runtime.InteropServices;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static class ImeCandidateAnchorReader
{
    private const uint InitCompForm = 0x10;
    private const uint CfsPoint = 0x02;
    private const uint CfsForcePosition = 0x20;
    private const uint CfsCandidatePos = 0x40;
    private const uint CfsExclude = 0x80;

    internal static unsafe (int X, int Y, int Top, string Source)? ReadCandidateForm(InputContext context)
    {
        // INPUTCONTEXT has no candidate-form initialization bit; index and style select list zero.
        var form = (CandidateForm*)context.CandidateForms;
        if (form->Index != 0)
        {
            return null;
        }

        return form->Style switch
        {
            CfsCandidatePos => (form->CurrentPosition.X, form->CurrentPosition.Y,
                form->CurrentPosition.Y, "candidate-position"),
            CfsExclude when form->Area.Left <= form->Area.Right && form->Area.Top <= form->Area.Bottom
                => (form->CurrentPosition.X, form->Area.Bottom, form->Area.Top, "candidate-exclude"),
            _ => null,
        };
    }

    internal static (int X, int Y, int Top, string Source)? ReadCompositionForm(InputContext context)
    {
        if ((context.FdwInit & InitCompForm) == 0)
        {
            return null;
        }

        var form = context.CompositionForm;
        // A layout region does not specify a caret. Use its lower-left edge without reading CurrentPosition.
        if (form.Style == 1 && form.Area.Left <= form.Area.Right && form.Area.Top <= form.Area.Bottom)
        {
            return (form.Area.Left, form.Area.Bottom, form.Area.Top, "composition-region");
        }

        if (form.Style is not (CfsPoint or CfsForcePosition))
        {
            return null;
        }

        var point = form.CurrentPosition;
        return (point.X, point.Y, point.Y, "composition-position");
    }

    private static unsafe ImeAnchorRectangle? ReadExclusion(InputContext context, string source)
    {
        var area = source == "candidate-exclude"
            ? ((CandidateForm*)context.CandidateForms)->Area
            : context.CompositionForm.Area;
        return source is "candidate-exclude" or "composition-region"
            ? new(area.Left, area.Top, area.Right, area.Bottom)
            : null;
    }

    internal static unsafe ImeCandidateAnchor? Read(HImc himc, ImeSessionId sessionId)
    {
        if (!OperatingSystem.IsWindows() || himc.Value == 0 || !ImeInputIdentity.UsesNativeContext)
        {
            return null;
        }

        var pointer = ImmContextAccessor.Instance.LockInputContext(himc);
        if (pointer == 0)
        {
            return null;
        }

        InputContext context;
        try
        {
            context = *(InputContext*)pointer;
        }
        finally
        {
            ImmContextAccessor.Instance.UnlockInputContext(himc);
        }

        var window = context.Hwnd.Value;
        var thread = InputTargetMethods.GetWindowThreadProcessId(window, out var process);
        var valid = InputTargetMethods.IsWindow(window) && process == (uint)Environment.ProcessId && thread != 0;
        var x = 0;
        var y = 0;
        var top = 0;
        var source = "unavailable";
        if (valid)
        {
            if (ReadCandidateForm(context) is { } candidate)
            {
                (x, y, top, source) = candidate;
            }
            else
            {
                var gui = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
                if (InputTargetMethods.GetGUIThreadInfo(thread, ref gui) && gui.Caret == window && gui.Focus == window)
                {
                    x = gui.CaretRect.Left;
                    y = gui.CaretRect.Bottom;
                    top = gui.CaretRect.Top;
                    source = "caret";
                }
                else if (ReadCompositionForm(context) is { } composition)
                {
                    (x, y, top, source) = composition;
                }
            }
        }

        var point = default(Point);
        var upper = default(Point);
        valid = valid && source != "unavailable"
                      && InputTargetMethods.TryGetPhysicalPoint(window, x, y, out point)
                      && InputTargetMethods.TryGetPhysicalPoint(window, x, top, out upper);
        return new
        (
            sessionId, window, process, thread, x, y, point.X, point.Y, upper.Y,
            valid ? InputTargetMethods.GetDpiForWindow(window) : 0, source, valid,
            valid ? null : "target-or-anchor-unavailable", top,
            ImeInputIdentity.WindowIdentityProperty,
            (long)InputTargetMethods.GetWindowProperty(window, ImeInputIdentity.WindowIdentityProperty),
            ImeInputIdentity.NextSequence(), ReadExclusion(context, source)
        );
    }
}