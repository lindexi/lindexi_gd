using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Persists immutable snapshots of courseware analysis runs with optimistic concurrency.
/// </summary>
public interface ICoursewareAnalysisRunStore
{
    /// <summary>
    /// Adds a newly created analysis run.
    /// </summary>
    ValueTask AddAsync(CoursewareAnalysisRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot of an analysis run, or <see langword="null" /> when it does not exist.
    /// </summary>
    ValueTask<CoursewareAnalysisRun?> GetAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the stored snapshot when its current version matches <paramref name="expectedVersion" />.
    /// </summary>
    ValueTask UpdateAsync(
        CoursewareAnalysisRun run,
        long expectedVersion,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Indicates that a persisted analysis run did not match the caller's expected version.
/// </summary>
public sealed class CoursewareAnalysisRunConcurrencyException : InvalidOperationException
{
    /// <summary>
    /// Initializes a concurrency exception for one analysis run.
    /// </summary>
    public CoursewareAnalysisRunConcurrencyException(string runId, long expectedVersion, long? actualVersion)
        : base(CreateMessage(runId, expectedVersion, actualVersion))
    {
        RunId = runId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    /// <summary>Gets the conflicting analysis run identifier.</summary>
    public string RunId { get; }

    /// <summary>Gets the version expected by the caller.</summary>
    public long ExpectedVersion { get; }

    /// <summary>Gets the version found in the store, or <see langword="null" /> when the run was absent.</summary>
    public long? ActualVersion { get; }

    private static string CreateMessage(string runId, long expectedVersion, long? actualVersion)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(runId));
        }

        return actualVersion is null
            ? $"Analysis run '{runId}' does not exist; expected version {expectedVersion}."
            : $"Analysis run '{runId}' is at version {actualVersion}; expected version {expectedVersion}.";
    }
}

/// <summary>
/// Provides a thread-safe in-memory analysis run store for composition and deterministic tests.
/// </summary>
public sealed class InMemoryCoursewareAnalysisRunStore : ICoursewareAnalysisRunStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, CoursewareAnalysisRun> _runs = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask AddAsync(CoursewareAnalysisRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        cancellationToken.ThrowIfCancellationRequested();
        if (run.Version != 0 || run.State != CoursewareAnalysisRunState.Created)
        {
            throw new ArgumentException("Only a newly created version-zero analysis run can be added.", nameof(run));
        }

        lock (_syncRoot)
        {
            if (_runs.TryGetValue(run.RunId, out var existingRun))
            {
                throw new CoursewareAnalysisRunConcurrencyException(run.RunId, expectedVersion: -1, existingRun.Version);
            }

            _runs.Add(run.RunId, run);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<CoursewareAnalysisRun?> GetAsync(string runId, CancellationToken cancellationToken = default)
    {
        ThrowIfNullOrWhiteSpace(runId, nameof(runId));
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _runs.TryGetValue(runId, out var run);
            return ValueTask.FromResult(run);
        }
    }

    /// <inheritdoc />
    public ValueTask UpdateAsync(
        CoursewareAnalysisRun run,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        cancellationToken.ThrowIfCancellationRequested();
        if (expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), expectedVersion, "Expected version cannot be negative.");
        }

        lock (_syncRoot)
        {
            if (!_runs.TryGetValue(run.RunId, out var currentRun))
            {
                throw new CoursewareAnalysisRunConcurrencyException(run.RunId, expectedVersion, actualVersion: null);
            }

            if (currentRun.Version != expectedVersion)
            {
                throw new CoursewareAnalysisRunConcurrencyException(run.RunId, expectedVersion, currentRun.Version);
            }

            if (run.Version != expectedVersion + 1)
            {
                throw new ArgumentException("The replacement run must be exactly one version newer than the stored snapshot.", nameof(run));
            }

            if (!string.Equals(run.InputFingerprint, currentRun.InputFingerprint, StringComparison.Ordinal))
            {
                throw new ArgumentException("An analysis run update cannot change the immutable input fingerprint.", nameof(run));
            }

            _runs[run.RunId] = run;
        }

        return ValueTask.CompletedTask;
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}
