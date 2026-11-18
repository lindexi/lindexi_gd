using System.Globalization;
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
        return new
        (
            context.PrepareFuzzyRulesChange(rules), CommittedText,
            rules.Aggregate(0, (effectiveMask, rule) => effectiveMask | (1 << (int)rule))
        );
    }

    /// <summary>Prepares a private replacement on a worker while the owning UI keeps input suspended.</summary>
    public Task<DictionaryDebugSession> PrepareSettingsChangeAsync
        (IImeDictionary? dictionary, int mask, CancellationToken cancellationToken) =>
        Task.Run
        (
            () =>
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
            }, cancellationToken
        );

    internal int CompositionCaret => context.Snapshot.Composition.CaretIndex;

    internal object ValidationState => new
    {
        engineCandidates = context.Snapshot.Candidates,
        contextmask = fuzzyRuleMask
    };

    public string CommittedText { get; private set; } = string.Empty;

    public CandidateWindowViewState Candidates => CandidateWindowStateMapper.Map
        (ImeUiState.FromSnapshot(context.Snapshot));

    /// <summary>
    /// Processes physical key events using the same core as the installed input method.
    /// </summary>
    public bool ProcessKey(Key key, KeyModifiers modifiers = KeyModifiers.None) => ProcessKey(key, modifiers, null);

    /// <summary>Processes a key with its layout-resolved symbol, falling back to standard key labels when unavailable.</summary>
    public bool ProcessKey(Key key, KeyModifiers modifiers, string? keySymbol)
    {
        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
        {
            return false;
        }

        var character = GetCharacter(key, modifiers, keySymbol);
        if (character is { } value)
        {
            var characterResult = context.ProcessKey(ImeKey.FromCharacter(value));
            CommittedText += characterResult.CommitText;
            // This read-only experience simulates the host receiving an unhandled character.
            if (!characterResult.Handled)
            {
                CommittedText += value;
            }

            return true;
        }

        ImeKey? input = key switch
        {
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
        {
            return false;
        }

        var result = context.ProcessKey(input.Value);
        CommittedText += result.CommitText;
        if (result.Handled)
        {
            return true;
        }

        // The read-only output cannot perform host editing for unhandled keys.
        switch (input.Value.Kind)
        {
            case ImeKeyKind.Space:
                CommittedText += ' ';
                return true;
            case ImeKeyKind.Backspace:
                if (CommittedText.Length > 0)
                {
                    var lastElementStart = StringInfo.ParseCombiningCharacters(CommittedText)[^1];
                    CommittedText = CommittedText[..lastElementStart];
                }

                return true;
            default:
                return false;
        }
    }

    internal static bool CanProcessKey(Key key, KeyModifiers modifiers, string? keySymbol) =>
        (modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 &&
        (GetCharacter(key, modifiers, keySymbol).HasValue || key is
            Key.Space or Key.Enter or Key.Back or Key.Escape or Key.Up or Key.Down or
            Key.PageUp or Key.PageDown or Key.Left or Key.Right or Key.Home or Key.End);

    private static char? GetCharacter(Key key, KeyModifiers modifiers, string? keySymbol)
    {
        if (keySymbol is { Length: 1 } && !char.IsControl(keySymbol[0]) && !char.IsWhiteSpace(keySymbol[0]))
        {
            return keySymbol[0];
        }

        var shift = (modifiers & KeyModifiers.Shift) != 0;
        return key switch
        {
            >= Key.A and <= Key.Z => (char)((shift ? 'A' : 'a') + key - Key.A),
            >= Key.D0 and <= Key.D9 => shift ? ")!@#$%^&*("[key - Key.D0] : (char)('0' + key - Key.D0),
            >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + key - Key.NumPad0),
            Key.OemSemicolon => shift ? ':' : ';',
            Key.OemQuestion => shift ? '?' : '/',
            Key.OemQuotes => shift ? '"' : '\'',
            Key.OemComma => shift ? '<' : ',',
            Key.OemPeriod => shift ? '>' : '.',
            Key.OemMinus => shift ? '_' : '-',
            Key.OemPlus => shift ? '+' : '=',
            Key.OemOpenBrackets => shift ? '{' : '[',
            Key.OemCloseBrackets => shift ? '}' : ']',
            Key.OemPipe => shift ? '|' : '\\',
            Key.OemTilde => shift ? '~' : '`',
            Key.Divide => '/',
            Key.Multiply => '*',
            Key.Subtract => '-',
            Key.Add => '+',
            Key.Decimal => '.',
            _ => null
        };
    }

    /// <summary>
    /// Cancels an unfinished composition when the debug window loses activation.
    /// </summary>
    public void CancelComposition() => context.ProcessKey(new ImeKey(ImeKeyKind.Escape));
}