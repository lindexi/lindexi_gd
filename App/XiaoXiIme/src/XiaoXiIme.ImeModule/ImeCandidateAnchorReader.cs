using System.Runtime.InteropServices;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static class ImeCandidateAnchorReader
{
    internal static unsafe ImeCandidateAnchor? Read(HImc himc, ImeSessionId sessionId)
    {
        if (!OperatingSystem.IsWindows() || himc.Value == 0)
            return null;
        var pointer = ImmContextAccessor.Instance.LockInputContext(himc);
        if (pointer == 0)
            return null;
        InputContext context;
        try { context = *(InputContext*)pointer; }
        finally { ImmContextAccessor.Instance.UnlockInputContext(himc); }

        var window = context.Hwnd.Value;
        var thread = InputTargetMethods.GetWindowThreadProcessId(window, out var process);
        var valid = InputTargetMethods.IsWindow(window) && process == (uint)Environment.ProcessId && thread != 0;
        var x = 0;
        var y = 0;
        var top = 0;
        var source = "unavailable";
        if (valid)
        {
            var form = (CandidateForm*)context.CandidateForms;
            if (form->Index == 0 && (form->Style == 0x40 || form->Style == 0x80))
            {
                x = form->CurrentPosition.X;
                y = form->Style == 0x80 ? form->Area.Bottom : form->CurrentPosition.Y;
                top = form->Style == 0x80 ? form->Area.Top : y;
                source = form->Style == 0x80 ? "candidate-exclude" : "candidate-position";
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
                else if (context.CompositionForm.Style is 1 or 2 or 0x20)
                {
                    x = context.CompositionForm.CurrentPosition.X;
                    y = context.CompositionForm.CurrentPosition.Y;
                    top = y;
                    source = "composition-position";
                }
            }
        }
        var point = default(Point);
        var upper = default(Point);
        valid = valid && source != "unavailable"
            && InputTargetMethods.TryGetPhysicalPoint(window, x, y, out point)
            && InputTargetMethods.TryGetPhysicalPoint(window, x, top, out upper);
        return new(sessionId, window, process, thread, x, y, point.X, point.Y, upper.Y,
            valid ? InputTargetMethods.GetDpiForWindow(window) : 0, source, valid,
            valid ? null : "target-or-anchor-unavailable", top);
    }
}
