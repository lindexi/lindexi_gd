namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Defines the persisted lifecycle states of one courseware analysis run.
/// </summary>
public enum CoursewareAnalysisRunState
{
    /// <summary>The run was created but input preparation has not started.</summary>
    Created,

    /// <summary>The immutable analysis input is being prepared.</summary>
    PreparingInput,

    /// <summary>The initial design-system candidate is being authored.</summary>
    Authoring,

    /// <summary>An immutable candidate is ready for quality evaluation.</summary>
    CandidateReady,

    /// <summary>The candidate is being evaluated by quality gates.</summary>
    Evaluating,

    /// <summary>Repair work is being planned from structured diagnostics.</summary>
    RepairPlanning,

    /// <summary>A bounded repair work item is being executed.</summary>
    Repairing,

    /// <summary>The candidate passed every required quality gate.</summary>
    Qualified,

    /// <summary>The qualified artifact is being published.</summary>
    Publishing,

    /// <summary>The artifact was atomically published and verified.</summary>
    Published,

    /// <summary>The run is waiting for information or a decision from the user.</summary>
    NeedsUserAction,

    /// <summary>The run is blocked by a temporarily unavailable dependency.</summary>
    InfrastructureBlocked,

    /// <summary>The run exhausted its repair or model budget without convergence.</summary>
    Exhausted,

    /// <summary>The run stopped because an internal invariant or implementation failed.</summary>
    ProductBug,

    /// <summary>The run was canceled by the user or host.</summary>
    Canceled,

    /// <summary>The run was replaced by a newer input or run.</summary>
    Superseded,

    /// <summary>The candidate qualified, but publication did not complete.</summary>
    PublicationFailed,
}

/// <summary>
/// Represents an immutable, persistable snapshot of one analysis workflow run.
/// </summary>
public sealed record CoursewareAnalysisRun
{
    private static readonly IReadOnlyDictionary<CoursewareAnalysisRunState, IReadOnlySet<CoursewareAnalysisRunState>> AllowedTransitions =
        new Dictionary<CoursewareAnalysisRunState, IReadOnlySet<CoursewareAnalysisRunState>>
        {
            [CoursewareAnalysisRunState.Created] = States(CoursewareAnalysisRunState.PreparingInput),
            [CoursewareAnalysisRunState.PreparingInput] = States(CoursewareAnalysisRunState.Authoring),
            [CoursewareAnalysisRunState.Authoring] = States(CoursewareAnalysisRunState.CandidateReady),
            [CoursewareAnalysisRunState.CandidateReady] = States(CoursewareAnalysisRunState.Evaluating),
            [CoursewareAnalysisRunState.Evaluating] = States(
                CoursewareAnalysisRunState.Qualified,
                CoursewareAnalysisRunState.RepairPlanning),
            [CoursewareAnalysisRunState.RepairPlanning] = States(CoursewareAnalysisRunState.Repairing),
            [CoursewareAnalysisRunState.Repairing] = States(CoursewareAnalysisRunState.CandidateReady),
            [CoursewareAnalysisRunState.Qualified] = States(CoursewareAnalysisRunState.Publishing),
            [CoursewareAnalysisRunState.Publishing] = States(
                CoursewareAnalysisRunState.Published,
                CoursewareAnalysisRunState.PublicationFailed),
            [CoursewareAnalysisRunState.PublicationFailed] = States(CoursewareAnalysisRunState.Publishing),
        };

    private static readonly IReadOnlySet<CoursewareAnalysisRunState> ActiveStates = States(
        CoursewareAnalysisRunState.Created,
        CoursewareAnalysisRunState.PreparingInput,
        CoursewareAnalysisRunState.Authoring,
        CoursewareAnalysisRunState.CandidateReady,
        CoursewareAnalysisRunState.Evaluating,
        CoursewareAnalysisRunState.RepairPlanning,
        CoursewareAnalysisRunState.Repairing,
        CoursewareAnalysisRunState.Qualified,
        CoursewareAnalysisRunState.Publishing,
        CoursewareAnalysisRunState.PublicationFailed);

    private static readonly IReadOnlySet<CoursewareAnalysisRunState> RecoverableInterruptionStates = States(
        CoursewareAnalysisRunState.NeedsUserAction,
        CoursewareAnalysisRunState.InfrastructureBlocked,
        CoursewareAnalysisRunState.Exhausted,
        CoursewareAnalysisRunState.ProductBug,
        CoursewareAnalysisRunState.Canceled);

    private static readonly IReadOnlySet<CoursewareAnalysisRunState> TerminalStates = States(
        CoursewareAnalysisRunState.Published,
        CoursewareAnalysisRunState.Superseded);

    private CoursewareAnalysisRun(
        string runId,
        string inputFingerprint,
        CoursewareAnalysisRunState state,
        CoursewareAnalysisRunState? resumeState,
        long version,
        string transitionReason,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        RunId = runId;
        InputFingerprint = inputFingerprint;
        State = state;
        ResumeState = resumeState;
        Version = version;
        TransitionReason = transitionReason;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets the stable run identifier.</summary>
    public string RunId { get; }

    /// <summary>Gets the immutable input fingerprint associated with the run.</summary>
    public string InputFingerprint { get; }

    /// <summary>Gets the current workflow state.</summary>
    public CoursewareAnalysisRunState State { get; }

    /// <summary>Gets the state to restore after a recoverable pause.</summary>
    public CoursewareAnalysisRunState? ResumeState { get; }

    /// <summary>Gets the optimistic-concurrency version.</summary>
    public long Version { get; }

    /// <summary>Gets the reason recorded for the most recent transition.</summary>
    public string TransitionReason { get; }

    /// <summary>Gets when the run was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets when the run last changed state.</summary>
    public DateTimeOffset UpdatedAt { get; }

    /// <summary>Gets whether the run is in a terminal state.</summary>
    public bool IsTerminal => TerminalStates.Contains(State);

    /// <summary>
    /// Creates a new analysis run for one immutable input snapshot.
    /// </summary>
    public static CoursewareAnalysisRun Create(string runId, string inputFingerprint, DateTimeOffset createdAt)
    {
        ThrowIfNullOrWhiteSpace(runId, nameof(runId));
        ThrowIfNullOrWhiteSpace(inputFingerprint, nameof(inputFingerprint));

        return new CoursewareAnalysisRun(
            runId,
            inputFingerprint,
            CoursewareAnalysisRunState.Created,
            resumeState: null,
            version: 0,
            transitionReason: "RunCreated",
            createdAt,
            createdAt);
    }

    /// <summary>
    /// Creates the next immutable run snapshot after validating the requested state transition.
    /// </summary>
    public CoursewareAnalysisRun TransitionTo(
        CoursewareAnalysisRunState targetState,
        string reason,
        DateTimeOffset transitionedAt)
    {
        ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (targetState == State)
        {
            throw new InvalidOperationException($"Analysis run is already in state '{State}'.");
        }

        if (!CanTransitionTo(targetState))
        {
            throw new InvalidOperationException($"Analysis run cannot transition from '{State}' to '{targetState}'.");
        }

        if (transitionedAt < UpdatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionedAt), transitionedAt, "Transition time cannot precede the previous update time.");
        }

        CoursewareAnalysisRunState? resumeState = IsRecoverableInterruption(targetState) ? State : null;
        return new CoursewareAnalysisRun(
            RunId,
            InputFingerprint,
            targetState,
            resumeState,
            Version + 1,
            reason,
            CreatedAt,
            transitionedAt);
    }

    /// <summary>
    /// Restores a run from a recoverable paused state to the state recorded before the pause.
    /// </summary>
    public CoursewareAnalysisRun Resume(string reason, DateTimeOffset resumedAt)
    {
        ThrowIfNullOrWhiteSpace(reason, nameof(reason));

        if (!IsRecoverableInterruption(State) || ResumeState is null)
        {
            throw new InvalidOperationException($"Analysis run in state '{State}' cannot be resumed.");
        }

        if (resumedAt < UpdatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(resumedAt), resumedAt, "Resume time cannot precede the previous update time.");
        }

        return new CoursewareAnalysisRun(
            RunId,
            InputFingerprint,
            ResumeState.Value,
            resumeState: null,
            Version + 1,
            reason,
            CreatedAt,
            resumedAt);
    }

    /// <summary>
    /// Determines whether the run can enter the requested state.
    /// </summary>
    public bool CanTransitionTo(CoursewareAnalysisRunState targetState)
    {
        if (targetState == State || IsTerminal)
        {
            return false;
        }

        if (IsRecoverableInterruption(State))
        {
            return targetState == CoursewareAnalysisRunState.Superseded;
        }

        if (targetState == CoursewareAnalysisRunState.Superseded)
        {
            return true;
        }

        if (targetState is CoursewareAnalysisRunState.NeedsUserAction
            or CoursewareAnalysisRunState.InfrastructureBlocked
            or CoursewareAnalysisRunState.Canceled)
        {
            return ActiveStates.Contains(State);
        }

        if (targetState == CoursewareAnalysisRunState.ProductBug)
        {
            return ActiveStates.Contains(State);
        }

        if (targetState == CoursewareAnalysisRunState.Exhausted)
        {
            return State is CoursewareAnalysisRunState.Authoring
                or CoursewareAnalysisRunState.Evaluating
                or CoursewareAnalysisRunState.RepairPlanning
                or CoursewareAnalysisRunState.Repairing;
        }

        return AllowedTransitions.TryGetValue(State, out var allowedStates)
            && allowedStates.Contains(targetState);
    }

    private static bool IsRecoverableInterruption(CoursewareAnalysisRunState state)
    {
        return RecoverableInterruptionStates.Contains(state);
    }

    private static IReadOnlySet<CoursewareAnalysisRunState> States(params CoursewareAnalysisRunState[] states)
    {
        return new HashSet<CoursewareAnalysisRunState>(states);
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}
