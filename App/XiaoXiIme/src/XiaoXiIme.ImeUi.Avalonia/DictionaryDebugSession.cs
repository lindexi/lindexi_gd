using Avalonia.Input;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;

namespace XiaoXiIme.ImeUi.Avalonia;

/// <summary>
/// Runs local dictionary input without an OS input context or external text injection.
/// </summary>
public sealed class DictionaryDebugSession(IImeDictionary dictionary)
{
    private readonly ImeContext context = new(dictionary);

    public string CommittedText { get; private set; } = string.Empty;
    public CandidateWindowViewState Candidates => CandidateWindowStateMapper.Map(ImeUiState.FromSnapshot(context.Snapshot));

    /// <summary>
    /// Processes physical key events using the same core as the installed input method.
    /// </summary>
    public bool ProcessKey(Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
            return false;

        ImeKey? input = key switch
        {
            >= Key.A and <= Key.Z => ImeKey.FromCharacter((char)('a' + key - Key.A)),
            >= Key.D1 and <= Key.D9 => ImeKey.SelectCandidate(key - Key.D1),
            >= Key.NumPad1 and <= Key.NumPad9 => ImeKey.SelectCandidate(key - Key.NumPad1),
            Key.OemQuestion or Key.Divide => ImeKey.FromCharacter('/'),
            Key.OemQuotes => ImeKey.FromCharacter('\''),
            Key.Space => new(ImeKeyKind.Space),
            Key.Enter => new(ImeKeyKind.Enter),
            Key.Back => new(ImeKeyKind.Backspace),
            Key.Escape => new(ImeKeyKind.Escape),
            Key.Up => ImeKey.PreviousCandidate(),
            Key.Down => ImeKey.NextCandidate(),
            Key.PageUp => ImeKey.PreviousCandidatePage(),
            Key.PageDown => ImeKey.NextCandidatePage(),
            Key.Left => ImeKey.MoveCompositionCaretLeft(),
            Key.Right => ImeKey.MoveCompositionCaretRight(),
            Key.Home => ImeKey.FirstCandidate(),
            Key.End => ImeKey.LastCandidate(),
            _ => null,
        };
        if (input is null)
            return false;

        var result = context.ProcessKey(input.Value);
        CommittedText += result.CommitText;
        return result.Handled;
    }

    /// <summary>
    /// Cancels an unfinished composition when the debug window loses activation.
    /// </summary>
    public void CancelComposition() => context.ProcessKey(new ImeKey(ImeKeyKind.Escape));
}
