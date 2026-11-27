using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private readonly IImeDictionary _dictionary;
    private readonly IShapeDictionary? _shapeDictionary;
    private readonly ISymbolDictionary? _symbolDictionary;
    private readonly Action<ImeDictionaryLearning>? _learn;
    private readonly List<ImeCandidate> _candidates = [];
    private string _reading = string.Empty;
    private int _caretIndex;
    private int _selection;
    private int _pageStart;
    private int _pageSize;
    private const int CandidatePageSize = 9;
    private const int MaxCandidateCount = 100;

    public ImeContext(IImeDictionary dictionary, Action<ImeDictionaryLearning>? learn = null)
    {
        _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        _shapeDictionary = dictionary as IShapeDictionary;
        _symbolDictionary = dictionary as ISymbolDictionary;
        _learn = learn;
        var lexicon = dictionary.GetLexicon();
        _options = new ImeInputOptions
        {
            CasePolicy = ImeShapeCasePolicy.UppercaseOnly,
            Placement = ImeShapePlacement.Both,
            LengthPolicy = ImeShapeLengthPolicy.OptionalOneOrTwo,
            EnableStandaloneShape = true
        };
        ValidateProjection(lexicon, _options.FuzzyRules);
    }

    public ImeSessionSnapshot Snapshot => CreateSnapshot();

    /// <summary>Prepares this session against another canonical dictionary without committing, learning, or mutation.</summary>
    public ImeContext PrepareDictionaryChange(IImeDictionary dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        var prepared = new ImeContext(dictionary, _options, _learn)
        {
            _reading = _reading,
            _caretIndex = _caretIndex,
            _selection = _selection,
            _eatConfirmationSpace = _eatConfirmationSpace,
            _revision = _revision + 1,
            _decodingIndex = ReferenceEquals(dictionary, _dictionary) ? _decodingIndex : null
        };
        prepared.RefreshCandidates();
        if (_candidates.Count > _selection)
        {
            var selected = _candidates[_selection];
            var index = prepared._candidates.FindIndex(c => c.Text == selected.Text && c.Reading == selected.Reading);
            prepared._selection = index >= 0 ? index : 0;
            prepared.NormalizeCandidateWindow();
        }

        return prepared;
    }

    /// <summary>Predicts key ownership from a published snapshot without changing composition or learning.</summary>
    public static bool ShouldProcessKey(ImeKey key, ImeSessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (key.Kind == ImeKeyKind.Other)
        {
            return false;
        }

        if (key.Kind != ImeKeyKind.Character)
        {
            return key.Kind switch
            {
                ImeKeyKind.Space when snapshot.EatsConfirmationSpace => true,
                ImeKeyKind.PreviousCandidate or ImeKeyKind.NextCandidate or
                    ImeKeyKind.PreviousCandidatePage or ImeKeyKind.NextCandidatePage or
                    ImeKeyKind.FirstCandidate
                    or ImeKeyKind.LastCandidate => snapshot.IsComposing && snapshot.Candidates.Count > 0,
                _ => snapshot.IsComposing
            };
        }

        if (snapshot.Composition.Reading.Length >= 64 &&
            (IsAsciiLetter(key.Character) || key.Character == ';' && snapshot.AcceptsSemicolon ||
             key.Character == '\'' && snapshot.AcceptsApostrophe && !snapshot.Composition.Reading.StartsWith('/') ||
             snapshot.Composition.Reading.StartsWith('/') && char.IsAsciiDigit(key.Character)))
        {
            return false;
        }

        var character = key.Character;
        if (IsCandidatePageCharacter(character))
        {
            return snapshot.IsComposing && snapshot.Candidates.Count > 0;
        }

        if (char.IsControl(character) || char.IsWhiteSpace(character))
        {
            return false;
        }

        return snapshot.IsComposing || IsAsciiLetter(character) || character == '/' ||
               character == ';' && snapshot.AcceptsSemicolon;
    }

    public ImeProcessResult ProcessKey(ImeKey key)
    {
        var eat = _eatConfirmationSpace && key.Kind == ImeKeyKind.Space;
        _eatConfirmationSpace = false;
        if (eat)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        var result = key.Kind switch
        {
            ImeKeyKind.Character => ProcessCharacter(key.Character),
            ImeKeyKind.Backspace => ProcessBackspace(),
            ImeKeyKind.Space => CommitCandidate(_selection),
            ImeKeyKind.Enter => CommitReading(),
            ImeKeyKind.Escape => ClearComposition(handled: IsComposing),
            ImeKeyKind.PreviousCandidate => MoveSelection(-1),
            ImeKeyKind.NextCandidate => MoveSelection(1),
            ImeKeyKind.PreviousCandidatePage => MoveSelection(-CandidatePageSize),
            ImeKeyKind.NextCandidatePage => MoveSelection(CandidatePageSize),
            ImeKeyKind.FirstCandidate => MoveSelectionTo(0),
            ImeKeyKind.LastCandidate => MoveSelectionTo(_candidates.Count - 1),
            ImeKeyKind.MoveCompositionCaretLeft => MoveCompositionCaret(-1),
            ImeKeyKind.MoveCompositionCaretRight => MoveCompositionCaret(1),
            ImeKeyKind.CandidateSelection => IsSymbolComposition
                ? ProcessSymbolDigit(key.CandidateIndex)
                : CommitCandidateInCurrentPage(key.CandidateIndex),
            _ => new ImeProcessResult(Snapshot, null, false)
        };
        return result with { Snapshot = Snapshot };
    }

    /// <summary>
    /// Replaces the current composition with the supplied input and refreshes candidates.
    /// An empty string cancels composition. Replacement does not trigger automatic commit.
    /// </summary>
    public ImeProcessResult SetComposition(string composition)
    {
        ArgumentNullException.ThrowIfNull(composition);
        if (composition == _reading)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        if (composition.Length > 64 || !IsValidComposition(composition))
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if (composition.Length == 0)
        {
            Clear();
            return new ImeProcessResult(Snapshot, null, true);
        }

        _reading = NormalizeComposition(composition);
        _caretIndex = _reading.Length;
        _selection = 0;
        RefreshCandidates();
        return new ImeProcessResult(Snapshot, null, true);
    }

    /// <summary>
    /// Queries conversion candidates for the supplied input without changing the current composition.
    /// </summary>
    public IReadOnlyList<ImeCandidate> QueryConversionList(string composition, int maxCount = 0)
    {
        ArgumentNullException.ThrowIfNull(composition);
        if (composition.Length == 0 || !IsValidComposition(composition))
        {
            return [];
        }

        var count = maxCount <= 0 ? int.MaxValue : maxCount;
        return QueryCandidates
        (
            NormalizeComposition(composition),
            count
        );
    }

    /// <summary>
    /// Queries reverse-conversion readings for the supplied candidate text without changing the current composition.
    /// </summary>
    public IReadOnlyList<ImeCandidate> QueryReverseConversionList(string text, int maxCount = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return [];
        }

        var count = maxCount <= 0 ? MaxCandidateCount : Math.Min(maxCount, MaxCandidateCount);
        return _dictionary.QueryByText(text, count);
    }

    /// <summary>
    /// Adds a phonetic user word without changing the current composition.
    /// </summary>
    public bool RegisterWord(string reading, string text)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(text);
        if (!TryCreateLearning(reading, text, out var learning) || _dictionary is not UserDictionary userDictionary)
        {
            return false;
        }

        userDictionary.Learn(learning);
        if (userDictionary.IsBackgroundPublicationEnabled)
        {
            userDictionary.PrepareAndPublish();
        }

        return true;
    }

    /// <summary>
    /// Removes a phonetic user word without changing the current composition.
    /// </summary>
    public bool UnregisterWord(string reading, string text)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(text);
        if (_dictionary is not UserDictionary userDictionary || !userDictionary.Forget(text, reading))
        {
            return false;
        }

        if (userDictionary.IsBackgroundPublicationEnabled)
        {
            userDictionary.PrepareAndPublish();
        }

        return true;
    }

    /// <summary>
    /// Enumerates phonetic user words without changing the current composition.
    /// </summary>
    public IReadOnlyList<UserDictionaryEntry> EnumerateRegisterWords(string? reading = null, string? text = null)
    {
        return _dictionary is UserDictionary userDictionary
            ? userDictionary.GetEntries(reading, text)
            : [];
    }

    private bool IsComposing => _reading.Length > 0;

    private bool IsSymbolComposition => _reading.Length > 0 && _reading[0] == '/';

    private static bool IsCandidatePageCharacter(char character) => character is '-' or '=';

    private ImeProcessResult ProcessCharacter(char character)
    {
        if (IsCandidatePageCharacter(character))
        {
            return MoveSelection(character == '-' ? -CandidatePageSize : CandidatePageSize);
        }

        if (character is >= '0' and <= '9')
        {
            var index = character == '0' ? 9 : character - '1';
            if (IsSymbolComposition)
            {
                return ProcessSymbolDigit(index);
            }

            if (_candidates.Count > 0)
            {
                return CommitCandidateInCurrentPage(index);
            }
        }

        if (!CanInsertCharacter(character) && !char.IsControl(character) && !char.IsWhiteSpace(character))
        {
            return CommitBeforeCharacter(character);
        }

        if (_reading.Length >= 64)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if (!CanInsertCharacter(character))
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        var appended = _caretIndex == _reading.Length;
        var normalizedCharacter = IsSymbolComposition || character == '/'
            ? char.ToLowerInvariant(character)
            : character;
        _reading = _reading.Insert(_caretIndex, normalizedCharacter.ToString());
        _caretIndex++;
        _selection = 0;
        RefreshCandidates();

        if (appended && TryAutomaticCommit() is { } automatic)
        {
            return automatic;
        }

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult CommitBeforeCharacter(char character)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        var text = new System.Text.StringBuilder();
        while (IsComposing)
        {
            var before = _reading.Length;
            var result = CommitCandidate(_selection);
            text.Append(result.CommitText);
            // Incomplete or unmatched input must be preserved rather than silently dropped.
            if (_reading.Length >= before)
            {
                text.Append(CommitReading().CommitText);
            }
        }

        text.Append(character);
        return new ImeProcessResult(Snapshot, text.ToString(), true);
    }

    private ImeProcessResult ProcessSymbolDigit(int candidateIndex)
    {
        if ((uint)candidateIndex >= 10)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        if (_reading.Length >= 64)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        var digit = candidateIndex == 9 ? '0' : (char)('1' + candidateIndex);
        _reading = _reading.Insert(_caretIndex, digit.ToString());
        _caretIndex++;
        _selection = 0;
        RefreshCandidates();
        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult ProcessBackspace()
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if (_caretIndex == 0)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        _reading = _reading.Remove(_caretIndex - 1, 1);
        _caretIndex--;
        RefreshCandidates();

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult MoveCompositionCaret(int delta)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        _caretIndex = Math.Clamp(_caretIndex + delta, 0, _reading.Length);

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult MoveSelection(int delta)
    {
        if (!IsComposing || _candidates.Count == 0)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        _selection = Math.Clamp(_selection + delta, 0, _candidates.Count - 1);
        NormalizeCandidateWindow();

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult MoveSelectionTo(int candidateIndex)
    {
        if (!IsComposing || _candidates.Count == 0)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        _selection = Math.Clamp(candidateIndex, 0, _candidates.Count - 1);
        NormalizeCandidateWindow();

        return new ImeProcessResult(Snapshot, null, true);
    }

    private ImeProcessResult CommitCandidate(int candidateIndex)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        // Another session can learn into the shared dictionary while this composition is inactive.
        if (!IsSymbolComposition && _dictionary is not UserDictionary { IsBackgroundPublicationEnabled: true } &&
            !ReferenceEquals(_displayedLexicon, _dictionary.GetLexicon()))
        {
            RefreshCandidates();
        }

        if ((uint)candidateIndex >= (uint)_candidates.Count)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        if (!IsSymbolComposition)
        {
            var detailed = _displayedCandidates[candidateIndex];
            return detailed.SelectionId is { } id ? SelectCandidate(id) : new ImeProcessResult(Snapshot, null, true);
        }

        var selectedCandidate = _candidates[candidateIndex];
        var originalInput = _reading;
        var shouldLearn = IsPhoneticComposition(originalInput);
        Clear();
        if (shouldLearn)
        {
            _learn?.Invoke(new ImeDictionaryLearning(selectedCandidate, originalInput));
        }

        return new ImeProcessResult(Snapshot, selectedCandidate.Text, true);
    }

    private ImeProcessResult CommitCandidateInCurrentPage(int pageCandidateIndex)
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        if ((uint)pageCandidateIndex >= (uint)_pageSize)
        {
            return new ImeProcessResult(Snapshot, null, true);
        }

        return CommitCandidate(_pageStart + pageCandidateIndex);
    }

    private ImeProcessResult CommitReading()
    {
        if (!IsComposing)
        {
            return new ImeProcessResult(Snapshot, null, false);
        }

        var commitText = _reading;
        Clear();

        return new ImeProcessResult(Snapshot, commitText, true);
    }

    private ImeProcessResult ClearComposition(bool handled)
    {
        Clear();

        return new ImeProcessResult(Snapshot, null, handled);
    }

    private void RefreshCandidates()
    {
        _candidates.Clear();
        _revision++;
        if (!IsComposing)
        {
            _displayedLexicon = null;
        }

        if (IsComposing && !IsSymbolComposition)
        {
            _displayedCandidates = Decode(_reading);
            _displayedLexicon = _decodedLexicon;
            _candidates.AddRange(_displayedCandidates.Select(c => c.Candidate));
            NormalizeCandidateWindow();
            return;
        }

        if (IsComposing)
        {
            _candidates.AddRange
            (
                QueryCandidates
                (
                    _reading,
                    int.MaxValue
                )
            );
        }

        NormalizeCandidateWindow();
    }

    private void Clear()
    {
        _revision++;
        _decodedCandidates = [];
        _displayedCandidates = [];
        _displayedLexicon = null;
        _decodedInput = null;
        _reading = string.Empty;
        _caretIndex = 0;
        _candidates.Clear();
        _selection = 0;
        _pageStart = 0;
        _pageSize = 0;
    }

    private void NormalizeCandidateWindow()
    {
        _caretIndex = Math.Clamp(_caretIndex, 0, _reading.Length);

        if (_candidates.Count == 0)
        {
            _selection = 0;
            _pageStart = 0;
            _pageSize = 0;
            return;
        }

        _selection = Math.Clamp(_selection, 0, _candidates.Count - 1);
        _pageStart = (_selection / CandidatePageSize) * CandidatePageSize;
        _pageSize = Math.Min(CandidatePageSize, _candidates.Count - _pageStart);
    }

    private ImeSessionSnapshot CreateSnapshot()
    {
        if (!IsComposing)
        {
            return ImeSessionSnapshot.Empty with
            {
                AcceptsSemicolon = IsPhoneticCharacter(';'),
                EatsConfirmationSpace = _eatConfirmationSpace,
                AcceptsApostrophe = IsPhoneticCharacter('\'')
            };
        }

        return new ImeSessionSnapshot
        (
            new CompositionText(_reading, _reading, _caretIndex),
            _candidates.ToArray(),
            CreateCandidateWindowState(),
            true,
            CreateGuideline(),
            IsPhoneticCharacter(';'),
            _eatConfirmationSpace,
            IsPhoneticCharacter('\'')
        );
    }

    private ImeGuideline CreateGuideline()
    {
        if (!IsComposing)
        {
            return ImeGuideline.Empty;
        }

        if (_candidates.Count == 0)
        {
            return new ImeGuideline(ImeGuidelineLevel.NoCandidate, $"无候选：{_reading}");
        }

        return new ImeGuideline(ImeGuidelineLevel.Reading, _reading);
    }

    private ImeCandidateWindowState CreateCandidateWindowState()
    {
        if (_candidates.Count == 0)
        {
            return ImeCandidateWindowState.Empty;
        }

        return new ImeCandidateWindowState
        (
            Selection: _selection,
            PageStart: _pageStart,
            PageSize: _pageSize
        );
    }

    private bool CanInsertCharacter(char character)
    {
        if (character == '\'')
        {
            return IsComposing && !IsSymbolComposition && IsPhoneticCharacter(character);
        }

        if (character == '/')
        {
            return !IsComposing;
        }

        return IsPhoneticCharacter(character);
    }

    private bool IsPhoneticCharacter(char character) => IsAsciiLetter(character) ||
                                                        character == '\'' && _dictionary.GetLexicon().Projection.Name ==
                                                        ImeProjectionDefinition.FullPinyin.Name ||
                                                        character == ';' && _dictionary.Projection?.Entries.Any
                                                            (entry => entry.Input.Contains(';')) == true;

    private bool IsValidComposition(string composition)
    {
        if (composition.Length == 0)
        {
            return true;
        }

        if (composition[0] == '/')
        {
            for (var index = 1; index < composition.Length; index++)
            {
                var character = composition[index];
                if (!IsAsciiLetter(character) && character is not (>= '0' and <= '9'))
                {
                    return false;
                }
            }

            return true;
        }

        for (var index = 0; index < composition.Length; index++)
        {
            if (!IsPhoneticCharacter(composition[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeComposition(string composition)
    {
        return composition.Length > 0 && composition[0] == '/'
            ? composition.ToLowerInvariant()
            : composition;
    }

    private IReadOnlyList<ImeCandidate> QueryCandidates
    (
        string composition,
        int maxCount
    )
    {
        if (composition.Length == 0)
        {
            return [];
        }

        if (composition[0] == '/')
        {
            return _symbolDictionary?.QuerySymbols(composition, maxCount) ?? [];
        }

        return Decode(composition).Take(maxCount).Select(c => c.Candidate).ToArray();
    }

    private static bool TryCreateLearning(string reading, string text, out ImeDictionaryLearning learning)
    {
        learning = null!;
        if (string.IsNullOrWhiteSpace(text)
            || text.Length > PhoneticDictionarySourceParser.MaxTextLength
            || string.IsNullOrWhiteSpace(reading))
        {
            return false;
        }

        var lookupKey = PhoneticDictionarySourceParser.NormalizeLookupKey(reading);
        if (lookupKey.Length == 0
            || lookupKey.Length > PhoneticDictionarySourceParser.MaxReadingLength
            || !IsPhoneticComposition(lookupKey))
        {
            return false;
        }

        learning = new ImeDictionaryLearning(new ImeCandidate(text, lookupKey), lookupKey);
        return true;
    }

    private static bool IsPhoneticComposition(string input)
    {
        return input.Length > 0 && input[0] != '/' && FindShapeStart(input) < 0;
    }

    private static int FindShapeStart(string input)
    {
        for (var index = 0; index < input.Length; index++)
        {
            if (input[index] is >= 'A' and <= 'Z')
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }
}