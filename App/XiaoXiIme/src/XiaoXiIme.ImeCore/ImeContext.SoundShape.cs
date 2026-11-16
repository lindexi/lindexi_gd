using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

public sealed partial class ImeContext
{
    private ImeInputOptions _options;
    private readonly Guid _contextId = Guid.NewGuid();
    private long _revision;
    private bool _eatConfirmationSpace;

    private ImeProcessResult? TryAutomaticCommit()
    {
        if (_options.AutoCommit == ImeAutoCommitPolicy.Off || _caretIndex != _reading.Length)
        {
            return null;
        }

        var complete = Decode(_reading).Where
        (c => c.SelectionId.HasValue &&
              c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete)
        ).ToArray();
        if (complete.Length == 0 || _options.AutoCommit == ImeAutoCommitPolicy.Unique && complete.Length != 1)
        {
            return null;
        }

        var candidate = _options.AutoCommit == ImeAutoCommitPolicy.Four
            ? complete.FirstOrDefault
            (c => c.Interpretations.Any
                (p =>
                    p.Kind == ImeInterpretationKind.Complete &&
                    p.Evidence.Count
                        (e => e.Kind == ImeEvidenceKind.PerCharacterShape && e.Range.Length == 2) ==
                    c.Candidate.Text.Length &&
                    p.CoverageEnd == c.Candidate.Text.Length * 4
                )
            )
            : complete[0];
        if (candidate is null)
        {
            return null;
        }

        var result = SelectCandidate(candidate.SelectionId.GetValueOrDefault());
        _eatConfirmationSpace = result.CommitText is not null && _options.EatConfirmationSpace;
        return result;
    }

    /// <summary>Creates an explicitly configured sound-shape context over canonical dictionary data.</summary>
    public ImeContext(IImeDictionary dictionary, ImeInputOptions options, Action<ImeDictionaryLearning>? learn)
        : this(dictionary, learn)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.FuzzyRules);
        var lexicon = dictionary.GetLexicon();
        if (!Enum.IsDefined(options.CasePolicy) || !Enum.IsDefined(options.Placement) ||
            !Enum.IsDefined(options.LengthPolicy)
            || !Enum.IsDefined(options.SortPolicy) || !Enum.IsDefined(options.AutoCommit)
            || options.FuzzyRules.Any(r => !Enum.IsDefined(r))
            || options.LengthPolicy == ImeShapeLengthPolicy.RequiredTwo &&
            options.Placement != ImeShapePlacement.PerCharacter
            || options.AutoCommit == ImeAutoCommitPolicy.Four &&
            (options.LengthPolicy != ImeShapeLengthPolicy.RequiredTwo || !lexicon.Projection.IsFixedTwoKey))
        {
            throw new ArgumentException(nameof(options), nameof(options));
        }

        _options = options with { FuzzyRules = Array.AsReadOnly(options.FuzzyRules.Distinct().ToArray()) };
        ValidateProjection(lexicon, _options.FuzzyRules);
    }

    /// <summary>Gets original character marks and the current detail page.</summary>
    public ImeDetailedSnapshot DetailedSnapshot => new
    (
        Snapshot, _reading.Select(char.IsUpper).ToArray(),
        QueryInterpretations(_reading, _pageStart, 9), _revision
    );

    /// <summary>Queries complete legal interpretations without modifying the composition.</summary>
    public ImeConversionPage QueryInterpretations(string composition, int offset = 0, int limit = 100)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        if (composition.Length > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(composition));
        }

        if (composition.Any(c => !IsPhoneticCharacter(c)))
        {
            throw new ArgumentException(nameof(composition), nameof(composition));
        }

        var results = Decode(composition);
        return new(results.Skip(offset).Take(limit).ToArray(), results.Count, offset, limit);
    }

    /// <summary>Queries shape aliases independently of the composition.</summary>
    public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 100)
    {
        ArgumentNullException.ThrowIfNull(shapeCode);
        if (maxCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount));
        }

        return (_shapeDictionary ?? throw new NotSupportedException()).QueryShape
            (shapeCode.ToLowerInvariant(), maxCount);
    }

    /// <summary>Selects a current semantic identity and consumes only its confirmed prefix.</summary>
    public ImeProcessResult SelectCandidate(ImeSelectionId selectionId)
    {
        if (selectionId.ContextId != _contextId || selectionId.Revision != _revision ||
            (_dictionary is not UserDictionary { IsBackgroundPublicationEnabled: true } &&
             !ReferenceEquals(_displayedLexicon, _dictionary.GetLexicon())))
        {
            return new(Snapshot, null, false);
        }

        var candidate = _displayedCandidates.SingleOrDefault(c => c.SelectionId == selectionId);
        if (candidate is null)
        {
            return new(Snapshot, null, false);
        }

        var path = candidate.Interpretations[0];
        if (path.ConsumedRange is not { } consumed)
        {
            return new(Snapshot, null, false);
        }

        var original = _reading[..consumed.End];
        _reading = _reading[consumed.End..];
        _caretIndex = Math.Max(0, _caretIndex - consumed.End);
        _selection = 0;
        RefreshCandidates();
        if (path.Segments.Count > 0)
        {
            _learn?.Invoke(new ImeDictionaryLearning(candidate.Candidate, original) { Segments = path.Segments });
        }

        return new(Snapshot, candidate.Candidate.Text, true);
    }

    /// <summary>Atomically replaces component rules and refreshes interpretations.</summary>
    public ImeProcessResult UpdateFuzzyRules(IReadOnlyList<ImeFuzzyRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Any(r => !Enum.IsDefined(r)))
        {
            throw new ArgumentException(nameof(rules), nameof(rules));
        }

        var copy = Array.AsReadOnly(rules.Distinct().ToArray());
        ValidateProjection(_dictionary.GetLexicon(), copy);
        _options = _options with { FuzzyRules = copy };
        RefreshCandidates();
        return new(Snapshot, null, true);
    }

    /// <summary>Clears composition-derived state without changing construction policies.</summary>
    public ImeProcessResult Reset()
    {
        _eatConfirmationSpace = false;
        Clear();
        _revision++;
        return new(Snapshot, null, true);
    }
}