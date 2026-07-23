using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Represents the result of committing one idempotent draft operation.
/// </summary>
public sealed class CoursewareDraftCommitResult
{
    /// <summary>
    /// Initializes one draft commit result.
    /// </summary>
    public CoursewareDraftCommitResult(bool wasCommitted, CoursewareDesignDraftRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        WasCommitted = wasCommitted;
        Revision = revision;
    }

    /// <summary>Gets whether this call created a new persisted revision.</summary>
    public bool WasCommitted { get; }

    /// <summary>Gets the persisted revision associated with the operation.</summary>
    public CoursewareDesignDraftRevision Revision { get; }
}

/// <summary>
/// Persists immutable design-draft revisions and idempotent operation identities.
/// </summary>
public interface ICoursewareDesignDraftStore
{
    /// <summary>
    /// Creates a draft with its initial revision.
    /// </summary>
    ValueTask CreateAsync(
        CoursewareDesignDraftRevision initialRevision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest revision of a draft, or <see langword="null" /> when the draft does not exist.
    /// </summary>
    ValueTask<CoursewareDesignDraftRevision?> GetLatestAsync(
        string draftId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all committed revisions in ascending revision order.
    /// </summary>
    ValueTask<IReadOnlyList<CoursewareDesignDraftRevision>> GetRevisionsAsync(
        string draftId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the next revision when the expected revision matches, or returns the prior result for the same operation.
    /// </summary>
    ValueTask<CoursewareDraftCommitResult> CommitAsync(
        string operationId,
        long expectedRevision,
        CoursewareDesignDraftRevision revision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful no-op operation against the expected immutable revision.
    /// </summary>
    ValueTask<CoursewareDraftCommitResult> RecordNoOpAsync(
        string operationId,
        long expectedRevision,
        CoursewareDesignDraftRevision revision,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Indicates that a design draft changed after the caller read it.
/// </summary>
public sealed class CoursewareDesignDraftConcurrencyException : InvalidOperationException
{
    /// <summary>
    /// Initializes a draft concurrency exception.
    /// </summary>
    public CoursewareDesignDraftConcurrencyException(string draftId, long expectedRevision, long? actualRevision)
        : base(CreateMessage(draftId, expectedRevision, actualRevision))
    {
        DraftId = draftId;
        ExpectedRevision = expectedRevision;
        ActualRevision = actualRevision;
    }

    /// <summary>Gets the conflicting draft identifier.</summary>
    public string DraftId { get; }

    /// <summary>Gets the revision expected by the caller.</summary>
    public long ExpectedRevision { get; }

    /// <summary>Gets the latest persisted revision, or <see langword="null" /> when the draft was absent.</summary>
    public long? ActualRevision { get; }

    private static string CreateMessage(string draftId, long expectedRevision, long? actualRevision)
    {
        if (string.IsNullOrWhiteSpace(draftId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(draftId));
        }

        return actualRevision is null
            ? $"Design draft '{draftId}' does not exist; expected revision {expectedRevision}."
            : $"Design draft '{draftId}' is at revision {actualRevision}; expected revision {expectedRevision}.";
    }
}

/// <summary>
/// Provides a thread-safe in-memory revision store for composition and deterministic tests.
/// </summary>
public sealed class InMemoryCoursewareDesignDraftStore : ICoursewareDesignDraftStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, List<CoursewareDesignDraftRevision>> _revisionsByDraftId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CommittedOperation> _operations = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask CreateAsync(
        CoursewareDesignDraftRevision initialRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(initialRevision);
        cancellationToken.ThrowIfCancellationRequested();
        if (initialRevision.Revision != 0 || initialRevision.ParentRevisionFingerprint is not null)
        {
            throw new ArgumentException("A draft must be created from an initial revision.", nameof(initialRevision));
        }

        lock (_syncRoot)
        {
            if (_revisionsByDraftId.TryGetValue(initialRevision.DraftId, out var existingRevisions))
            {
                throw new CoursewareDesignDraftConcurrencyException(
                    initialRevision.DraftId,
                    expectedRevision: -1,
                    existingRevisions[^1].Revision);
            }

            _revisionsByDraftId.Add(initialRevision.DraftId, [initialRevision]);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<CoursewareDesignDraftRevision?> GetLatestAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(draftId, nameof(draftId));
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            return ValueTask.FromResult(
                _revisionsByDraftId.TryGetValue(draftId, out var revisions)
                    ? revisions[^1]
                    : null);
        }
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<CoursewareDesignDraftRevision>> GetRevisionsAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(draftId, nameof(draftId));
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            IReadOnlyList<CoursewareDesignDraftRevision> revisions = _revisionsByDraftId.TryGetValue(draftId, out var storedRevisions)
                ? storedRevisions.ToArray()
                : [];
            return ValueTask.FromResult(revisions);
        }
    }

    /// <inheritdoc />
    public ValueTask<CoursewareDraftCommitResult> CommitAsync(
        string operationId,
        long expectedRevision,
        CoursewareDesignDraftRevision revision,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(operationId, nameof(operationId));
        ArgumentNullException.ThrowIfNull(revision);
        cancellationToken.ThrowIfCancellationRequested();
        if (expectedRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedRevision), expectedRevision, "Expected revision cannot be negative.");
        }

        lock (_syncRoot)
        {
            if (_operations.TryGetValue(operationId, out var committedOperation))
            {
                if (!string.Equals(committedOperation.DraftId, revision.DraftId, StringComparison.Ordinal)
                    || !string.Equals(
                        committedOperation.RevisionFingerprint,
                        revision.RevisionFingerprint,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Operation '{operationId}' was already used for a different draft revision.");
                }

                return ValueTask.FromResult(new CoursewareDraftCommitResult(
                    wasCommitted: false,
                    committedOperation.Revision));
            }

            if (!_revisionsByDraftId.TryGetValue(revision.DraftId, out var revisions))
            {
                throw new CoursewareDesignDraftConcurrencyException(revision.DraftId, expectedRevision, actualRevision: null);
            }

            var latestRevision = revisions[^1];
            if (latestRevision.Revision != expectedRevision)
            {
                throw new CoursewareDesignDraftConcurrencyException(
                    revision.DraftId,
                    expectedRevision,
                    latestRevision.Revision);
            }

            if (revision.Revision != expectedRevision + 1
                || !string.Equals(
                    revision.ParentRevisionFingerprint,
                    latestRevision.RevisionFingerprint,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException("The committed revision must directly descend from the latest persisted revision.", nameof(revision));
            }

            revisions.Add(revision);
            _operations.Add(operationId, new CommittedOperation(
                revision.DraftId,
                revision.RevisionFingerprint,
                revision));
            return ValueTask.FromResult(new CoursewareDraftCommitResult(
                wasCommitted: true,
                revision));
        }
    }

    /// <inheritdoc />
    public ValueTask<CoursewareDraftCommitResult> RecordNoOpAsync(
        string operationId,
        long expectedRevision,
        CoursewareDesignDraftRevision revision,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(operationId, nameof(operationId));
        ArgumentNullException.ThrowIfNull(revision);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            if (_operations.TryGetValue(operationId, out var committedOperation))
            {
                if (!string.Equals(committedOperation.DraftId, revision.DraftId, StringComparison.Ordinal)
                    || !string.Equals(committedOperation.RevisionFingerprint, revision.RevisionFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Operation '{operationId}' was already used for a different draft revision.");
                }

                return ValueTask.FromResult(new CoursewareDraftCommitResult(wasCommitted: false, committedOperation.Revision));
            }

            if (!_revisionsByDraftId.TryGetValue(revision.DraftId, out var revisions))
            {
                throw new CoursewareDesignDraftConcurrencyException(revision.DraftId, expectedRevision, actualRevision: null);
            }

            var latestRevision = revisions[^1];
            if (latestRevision.Revision != expectedRevision
                || !string.Equals(latestRevision.RevisionFingerprint, revision.RevisionFingerprint, StringComparison.Ordinal))
            {
                throw new CoursewareDesignDraftConcurrencyException(revision.DraftId, expectedRevision, latestRevision.Revision);
            }

            _operations.Add(operationId, new CommittedOperation(
                revision.DraftId,
                revision.RevisionFingerprint,
                revision));
            return ValueTask.FromResult(new CoursewareDraftCommitResult(wasCommitted: false, revision));
        }
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }

    private sealed class CommittedOperation
    {
        public CommittedOperation(
            string draftId,
            string revisionFingerprint,
            CoursewareDesignDraftRevision revision)
        {
            DraftId = draftId;
            RevisionFingerprint = revisionFingerprint;
            Revision = revision;
        }

        public string DraftId { get; }

        public string RevisionFingerprint { get; }

        public CoursewareDesignDraftRevision Revision { get; }
    }
}
