namespace LightTextEditorPlus.Core.Document.Utils;

static class ImmutableRunHelper
{
    public static bool IsEndWithBreakLine(IImmutableRun run)
    {
        if (run is TextRun textRun)
        {
            return IsEndWithBreakLine((IImmutableRunList) textRun);
        }

        if (run.Count == 0)
        {
            return false;
        }

        ICharObject charObject = run.GetChar(run.Count - 1);
        return IsCharObjectEndWithBreakLine(charObject);
    }

    public static bool IsEndWithBreakLine(IImmutableRunList? runList)
    {
        if (runList is null || runList.RunCount == 0)
        {
            return false;
        }

        IImmutableRun run = runList.GetRun(runList.RunCount - 1);
        if (run is LineBreakRun)
        {
            return true;
        }

        if (run.Count == 0)
        {
            return false;
        }

        if (run is TextRun textRun)
        {
            return IsTextEndWithBreakLine(textRun.Text);
        }

        ICharObject charObject = run.GetChar(run.Count - 1);
        return IsCharObjectEndWithBreakLine(charObject);
    }

    private static bool IsCharObjectEndWithBreakLine(ICharObject charObject)
    {
        if (ReferenceEquals(charObject, LineBreakCharObject.Instance))
        {
            return true;
        }
        string text = charObject.ToText();
        return IsTextEndWithBreakLine(text);
    }
    private static bool IsTextEndWithBreakLine(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.EndsWith('\r') || text.EndsWith("\n");
    }
}