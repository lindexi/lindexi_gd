using System.Security.Cryptography;
using System.Text;

namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Identifies an addressable entity kind in a courseware design draft.
/// </summary>
public enum CoursewareDraftEntityKind
{
    /// <summary>A spacing token.</summary>
    SpacingToken,

    /// <summary>A typography token.</summary>
    TypographyToken,

    /// <summary>A color token.</summary>
    ColorToken,

    /// <summary>An effect token.</summary>
    EffectToken,

    /// <summary>A reusable component.</summary>
    Component,

    /// <summary>A page-type contract.</summary>
    PageType,

    /// <summary>A slide-to-page-type assignment.</summary>
    Assignment,

    /// <summary>A page template.</summary>
    Template,
}

/// <summary>
/// Identifies an entity independently from its model-authored stable identifier.
/// </summary>
public readonly record struct CoursewareDraftEntityKey
{
    private CoursewareDraftEntityKey(string value)
    {
        Value = value;
    }

    /// <summary>Gets the host-generated key value.</summary>
    public string Value { get; }

    /// <summary>Creates a new host-generated entity key.</summary>
    public static CoursewareDraftEntityKey Create()
    {
        return new CoursewareDraftEntityKey($"entity-{Guid.NewGuid():N}");
    }

    internal static CoursewareDraftEntityKey Create(string operationId, int operationIndex)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(operationId));
        }

        if (operationIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(operationIndex), operationIndex, "Operation index cannot be negative.");
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{operationId}\0{operationIndex}"));
        return new CoursewareDraftEntityKey($"entity-{Convert.ToHexString(hash).ToLowerInvariant()}");
    }

    /// <summary>Restores a previously persisted entity key.</summary>
    public static CoursewareDraftEntityKey Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        }

        return new CoursewareDraftEntityKey(value);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Describes one persisted mapping between a host entity key and a model stable identifier.
/// </summary>
public sealed record CoursewareDraftEntityIdentity
{
    /// <summary>Gets the entity kind.</summary>
    public CoursewareDraftEntityKind Kind { get; init; }

    /// <summary>Gets the host-generated entity key.</summary>
    public string EntityKey { get; init; } = string.Empty;

    /// <summary>Gets the current model stable identifier.</summary>
    public string StableId { get; init; } = string.Empty;
}

/// <summary>
/// Contains exactly one strongly typed draft entity payload.
/// </summary>
public sealed record CoursewareDraftEntityPayload
{
    /// <summary>Gets a spacing-token payload.</summary>
    public CoursewareSpacingToken? SpacingToken { get; init; }

    /// <summary>Gets a typography-token payload.</summary>
    public CoursewareTypographyToken? TypographyToken { get; init; }

    /// <summary>Gets a color-token payload.</summary>
    public CoursewareColorToken? ColorToken { get; init; }

    /// <summary>Gets an effect-token payload.</summary>
    public CoursewareEffectToken? EffectToken { get; init; }

    /// <summary>Gets a component payload.</summary>
    public CoursewareComponentSpecification? Component { get; init; }

    /// <summary>Gets a page-type payload.</summary>
    public CoursewarePageTypeContract? PageType { get; init; }

    /// <summary>Gets an assignment payload.</summary>
    public CoursewarePageTypeAssignment? Assignment { get; init; }

    /// <summary>Gets a template payload.</summary>
    public CoursewarePageTemplate? Template { get; init; }
}

/// <summary>
/// Identifies the mutation requested for one draft entity.
/// </summary>
public enum CoursewareDraftOperationKind
{
    /// <summary>Adds a new entity and allocates an entity key.</summary>
    Add,

    /// <summary>Replaces an entity addressed by its entity key.</summary>
    Replace,

    /// <summary>Deletes an entity addressed by its entity key.</summary>
    Delete,

    /// <summary>Renames an entity stable identifier and updates direct references.</summary>
    RenameStableId,
}

/// <summary>
/// Describes one mutation within an atomic draft transaction.
/// </summary>
public sealed record CoursewareDraftOperation
{
    /// <summary>Gets the operation kind.</summary>
    public CoursewareDraftOperationKind OperationKind { get; init; }

    /// <summary>Gets the entity kind.</summary>
    public CoursewareDraftEntityKind EntityKind { get; init; }

    /// <summary>Gets the target entity key for non-add operations.</summary>
    public CoursewareDraftEntityKey? EntityKey { get; init; }

    /// <summary>Gets the entity payload for add or replace operations.</summary>
    public CoursewareDraftEntityPayload? Payload { get; init; }

    /// <summary>Gets the new stable identifier for a rename operation.</summary>
    public string? NewStableId { get; init; }
}

/// <summary>
/// Defines permissions granted to one repair work item.
/// </summary>
public sealed record CoursewareDraftWorkItemScope
{
    /// <summary>Gets the entity kinds that may be changed.</summary>
    public IReadOnlySet<CoursewareDraftEntityKind> AllowedEntityKinds { get; init; }
        = new HashSet<CoursewareDraftEntityKind>();

    /// <summary>Gets explicitly allowed existing entity keys; an empty set allows any key of an allowed kind.</summary>
    public IReadOnlySet<string> AllowedEntityKeys { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the allowed operation kinds.</summary>
    public IReadOnlySet<CoursewareDraftOperationKind> AllowedOperationKinds { get; init; }
        = new HashSet<CoursewareDraftOperationKind>();
}

/// <summary>
/// Describes the authorization context for one draft operation batch.
/// </summary>
public sealed record CoursewareDraftOperationScope
{
    /// <summary>Gets the repair work-item identifier.</summary>
    public string WorkItemId { get; init; } = string.Empty;

    /// <summary>Gets the permissions granted to the work item.</summary>
    public CoursewareDraftWorkItemScope Permissions { get; init; } = new();
}

/// <summary>
/// Describes one atomic draft mutation request.
/// </summary>
public sealed record CoursewareDraftMutationRequest
{
    /// <summary>Gets the idempotency key.</summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>Gets the target draft identifier.</summary>
    public string DraftId { get; init; } = string.Empty;

    /// <summary>Gets the revision expected by the caller.</summary>
    public long ExpectedRevision { get; init; }

    /// <summary>Gets the authorization scope.</summary>
    public CoursewareDraftOperationScope Scope { get; init; } = new();

    /// <summary>Gets the operations committed as one transaction.</summary>
    public IReadOnlyList<CoursewareDraftOperation> Operations { get; init; } = [];

    /// <summary>Gets when a successful revision should be created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Describes one addressable entity in a draft manifest.
/// </summary>
public sealed record CoursewareDraftManifestEntry
{
    /// <summary>Gets the entity kind.</summary>
    public CoursewareDraftEntityKind Kind { get; init; }

    /// <summary>Gets the host entity key.</summary>
    public CoursewareDraftEntityKey EntityKey { get; init; }

    /// <summary>Gets the current model stable identifier.</summary>
    public string StableId { get; init; } = string.Empty;
}

/// <summary>
/// Summarizes the addressable content of one immutable draft revision.
/// </summary>
public sealed record CoursewareDraftManifest
{
    /// <summary>Gets the draft identifier.</summary>
    public string DraftId { get; init; } = string.Empty;

    /// <summary>Gets the revision number.</summary>
    public long Revision { get; init; }

    /// <summary>Gets the revision fingerprint.</summary>
    public string RevisionFingerprint { get; init; } = string.Empty;

    /// <summary>Gets all addressable entities.</summary>
    public IReadOnlyList<CoursewareDraftManifestEntry> Entities { get; init; } = [];
}

/// <summary>
/// Contains an isolated snapshot of one draft section or entity.
/// </summary>
public sealed record CoursewareDraftSectionSnapshot
{
    /// <summary>Gets the draft identifier.</summary>
    public string DraftId { get; init; } = string.Empty;

    /// <summary>Gets the revision number.</summary>
    public long Revision { get; init; }

    /// <summary>Gets the entity kind.</summary>
    public CoursewareDraftEntityKind EntityKind { get; init; }

    /// <summary>Gets the entity key when a single entity was requested.</summary>
    public CoursewareDraftEntityKey? EntityKey { get; init; }

    /// <summary>Gets the matching isolated payloads.</summary>
    public IReadOnlyList<CoursewareDraftEntityPayload> Entities { get; init; } = [];
}

/// <summary>
/// Describes the outcome of one draft mutation request.
/// </summary>
public sealed record CoursewareDraftMutationResult
{
    /// <summary>Gets whether the request succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets whether this call persisted a new revision.</summary>
    public bool WasCommitted { get; init; }

    /// <summary>Gets whether the request produced no design-system change.</summary>
    public bool WasNoOp { get; init; }

    /// <summary>Gets the resulting revision.</summary>
    public CoursewareDesignDraftRevision Revision { get; init; } = null!;

    /// <summary>Gets entity keys allocated by add operations in operation order.</summary>
    public IReadOnlyList<CoursewareDraftEntityKey> CreatedEntityKeys { get; init; } = [];

    /// <summary>Gets deterministic diagnostics when the request is rejected.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
/// Describes a candidate proposal without freezing, qualifying or publishing it.
/// </summary>
public sealed record CoursewareDraftCandidateProposal
{
    /// <summary>Gets the proposed candidate.</summary>
    public CoursewareCandidate Candidate { get; init; } = null!;

    /// <summary>Gets the draft manifest associated with the candidate revision.</summary>
    public CoursewareDraftManifest Manifest { get; init; } = new();
}
