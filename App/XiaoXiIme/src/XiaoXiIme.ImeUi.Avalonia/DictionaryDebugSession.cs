using Avalonia.Input;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;

namespace XiaoXiIme.ImeUi.Avalonia;

/// <summary>
/// Runs local dictionary input without an OS input context or external text injection.
/// </summary>
public sealed class DictionaryDebugSession
{
    private readonly ImeContext context;
    private readonly int fuzzyRuleMask;

    /// <summary>Creates a local session over canonical dictionary data.</summary>
    public DictionaryDebugSession(IImeDictionary dictionary) => context = new(dictionary);

    private DictionaryDebugSession(ImeContext prepared, string committedText, int fuzzyRuleMask)
    {
        context = prepared;
        CommittedText = committedText;
        this.fuzzyRuleMask = fuzzyRuleMask;
    }

    /// <summary>Prepares a replacement session while retaining output and exact active input without replaying keys.</summary>
    public DictionaryDebugSession PrepareDictionaryChange(IImeDictionary dictionary) =>
        new(context.PrepareDictionaryChange(dictionary), CommittedText, fuzzyRuleMask);

    /// <summary>Prepares fuzzy candidates while retaining input, caret, and committed output.</summary>
    public DictionaryDebugSession PrepareFuzzyRulesChange(int mask)
    {
        var rules = Enum.GetValues<ImeFuzzyRule>().Where(r => (mask & (1 << (int)r)) != 0).ToArray();
        return new(context.PrepareFuzzyRulesChange(rules), CommittedText,
            rules.Aggregate(0, (effectiveMask, rule) => effectiveMask | (1 << (int)rule)));
    }

    /// <summary>Prepares a private replacement on a worker while the owning UI keeps input suspended.</summary>
    public Task<DictionaryDebugSession> PrepareSettingsChangeAsync(IImeDictionary? dictionary, int mask, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            SettingsApplyTrace.Mark("worker-prepare-start", started);
            var prepared = dictionary is null ? this : PrepareDictionaryChange(dictionary);
            cancellationToken.ThrowIfCancellationRequested();
            SettingsApplyTrace.Mark("worker-fuzzy-start", started);
            prepared = prepared.PrepareFuzzyRulesChange(mask);
            SettingsApplyTrace.Mark("worker-fuzzy-end", started);
            cancellationToken.ThrowIfCancellationRequested();
            return prepared;
        }, cancellationToken);

    internal int CompositionCaret => context.Snapshot.Composition.CaretIndex;
    internal object ValidationState => new
    {
        engineCandidates = context.Snapshot.Candidates,
        contextmask = fuzzyRuleMask
    };
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
            Key.OemSemicolon when (modifiers & KeyModifiers.Shift) == 0 => ImeKey.FromCharacter(';'),
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
