using System.Security.Cryptography;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Serialization;

namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Represents an immutable design-system draft revision whose payload is isolated from external mutation.
/// </summary>
public sealed record CoursewareDesignDraftRevision
{
    /// <summary>The fingerprint algorithm and canonical serialization version.</summary>
    public const string FingerprintAlgorithmVersion = "sha256-json-v1";

    private readonly byte[] _canonicalDesignSystemJson;
    private readonly byte[] _canonicalEntityIdentitiesJson;

    private CoursewareDesignDraftRevision(
        string draftId,
        long revision,
        string? parentRevisionFingerprint,
        string revisionFingerprint,
        DateTimeOffset createdAt,
        byte[] canonicalDesignSystemJson,
        byte[] canonicalEntityIdentitiesJson)
    {
        DraftId = draftId;
        Revision = revision;
        ParentRevisionFingerprint = parentRevisionFingerprint;
        RevisionFingerprint = revisionFingerprint;
        CreatedAt = createdAt;
        _canonicalDesignSystemJson = canonicalDesignSystemJson;
        _canonicalEntityIdentitiesJson = canonicalEntityIdentitiesJson;
    }

    /// <summary>Gets the stable draft identifier.</summary>
    public string DraftId { get; }

    /// <summary>Gets the monotonically increasing revision number.</summary>
    public long Revision { get; }

    /// <summary>Gets the fingerprint of the parent revision, if one exists.</summary>
    public string? ParentRevisionFingerprint { get; }

    /// <summary>Gets the deterministic fingerprint of this revision.</summary>
    public string RevisionFingerprint { get; }

    /// <summary>Gets when this revision was committed.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets an isolated design-system snapshot.</summary>
    public CoursewareDesignSystem DesignSystem => DeserializeDesignSystem(_canonicalDesignSystemJson);

    /// <summary>Gets isolated host-managed entity identities.</summary>
    public IReadOnlyList<CoursewareDraftEntityIdentity> EntityIdentities => DeserializeEntityIdentities(_canonicalEntityIdentitiesJson);

    /// <summary>
    /// Creates the first immutable revision for a draft.
    /// </summary>
    public static CoursewareDesignDraftRevision CreateInitial(
        string draftId,
        CoursewareDesignSystem designSystem,
        DateTimeOffset createdAt)
    {
        ThrowIfNullOrWhiteSpace(draftId, nameof(draftId));
        ArgumentNullException.ThrowIfNull(designSystem);
        CoursewareDraftEntityIdentityFactory.ValidateStableIds(designSystem);

        return Create(
            draftId,
            revision: 0,
            parentRevisionFingerprint: null,
            designSystem,
            CoursewareDraftEntityIdentityFactory.Create(designSystem),
            createdAt);
    }

    /// <summary>
    /// Creates the next immutable revision without modifying the current revision.
    /// </summary>
    public CoursewareDesignDraftRevision CreateNext(
        CoursewareDesignSystem designSystem,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        if (createdAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAt), createdAt, "Revision time cannot precede its parent revision.");
        }

        if (HasSameDesignSystem(designSystem))
        {
            throw new InvalidOperationException("A new draft revision must change the design-system payload.");
        }

        return Create(DraftId, Revision + 1, RevisionFingerprint, designSystem, EntityIdentities, createdAt);
    }

    /// <summary>
    /// Creates the next immutable revision with updated host-managed entity identities.
    /// </summary>
    public CoursewareDesignDraftRevision CreateNext(
        CoursewareDesignSystem designSystem,
        IReadOnlyList<CoursewareDraftEntityIdentity> entityIdentities,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        ArgumentNullException.ThrowIfNull(entityIdentities);
        if (createdAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAt), createdAt, "Revision time cannot precede its parent revision.");
        }

        if (HasSamePayload(designSystem, entityIdentities))
        {
            throw new InvalidOperationException("A new draft revision must change the design-system payload or entity identities.");
        }

        return Create(DraftId, Revision + 1, RevisionFingerprint, designSystem, entityIdentities, createdAt);
    }

    /// <summary>
    /// Determines whether the supplied design system has the same canonical payload as this revision.
    /// </summary>
    public bool HasSameDesignSystem(CoursewareDesignSystem designSystem)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        var candidateJson = SerializeDesignSystem(designSystem);
        return _canonicalDesignSystemJson.AsSpan().SequenceEqual(candidateJson);
    }

    /// <summary>
    /// Determines whether both the design system and host entity identities match this revision.
    /// </summary>
    public bool HasSamePayload(
        CoursewareDesignSystem designSystem,
        IReadOnlyList<CoursewareDraftEntityIdentity> entityIdentities)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        ArgumentNullException.ThrowIfNull(entityIdentities);
        return HasSameDesignSystem(designSystem)
            && _canonicalEntityIdentitiesJson.AsSpan().SequenceEqual(SerializeEntityIdentities(entityIdentities));
    }

    internal ReadOnlyMemory<byte> GetCanonicalDesignSystemJson()
    {
        return _canonicalDesignSystemJson.ToArray();
    }

    private static CoursewareDesignDraftRevision Create(
        string draftId,
        long revision,
        string? parentRevisionFingerprint,
        CoursewareDesignSystem designSystem,
        IReadOnlyList<CoursewareDraftEntityIdentity> entityIdentities,
        DateTimeOffset createdAt)
    {
        var canonicalDesignSystemJson = SerializeDesignSystem(designSystem);
        var canonicalEntityIdentitiesJson = SerializeEntityIdentities(entityIdentities);
        var revisionFingerprint = ComputeFingerprint(
            draftId,
            revision,
            parentRevisionFingerprint,
            canonicalDesignSystemJson,
            canonicalEntityIdentitiesJson);

        return new CoursewareDesignDraftRevision(
            draftId,
            revision,
            parentRevisionFingerprint,
            revisionFingerprint,
            createdAt,
            canonicalDesignSystemJson,
            canonicalEntityIdentitiesJson);
    }

    private static string ComputeFingerprint(
        string draftId,
        long revision,
        string? parentRevisionFingerprint,
        ReadOnlySpan<byte> canonicalDesignSystemJson,
        ReadOnlySpan<byte> canonicalEntityIdentitiesJson)
    {
        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendString(incrementalHash, FingerprintAlgorithmVersion);
        AppendString(incrementalHash, draftId);
        AppendString(incrementalHash, revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AppendString(incrementalHash, parentRevisionFingerprint ?? string.Empty);
        incrementalHash.AppendData(canonicalDesignSystemJson);
        incrementalHash.AppendData([0]);
        incrementalHash.AppendData(canonicalEntityIdentitiesJson);
        return Convert.ToHexString(incrementalHash.GetHashAndReset()).ToLowerInvariant();
    }

    private static void AppendString(IncrementalHash incrementalHash, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        incrementalHash.AppendData(bytes);
        incrementalHash.AppendData([0]);
    }

    private static byte[] SerializeDesignSystem(CoursewareDesignSystem designSystem)
    {
        return JsonSerializer.SerializeToUtf8Bytes(
            designSystem,
            CoursewareDesignJsonSerializerContext.Default.CoursewareDesignSystem);
    }

    private static CoursewareDesignSystem DeserializeDesignSystem(ReadOnlySpan<byte> json)
    {
        return JsonSerializer.Deserialize(
                json,
                CoursewareDesignJsonSerializerContext.Default.CoursewareDesignSystem)
            ?? throw new InvalidOperationException("The stored design-system revision payload could not be deserialized.");
    }

    private static byte[] SerializeEntityIdentities(IReadOnlyList<CoursewareDraftEntityIdentity> entityIdentities)
    {
        var orderedIdentities = entityIdentities
            .OrderBy(identity => identity.Kind)
            .ThenBy(identity => identity.EntityKey, StringComparer.Ordinal)
            .ToArray();
        return JsonSerializer.SerializeToUtf8Bytes(
            orderedIdentities,
            CoursewareDesignJsonSerializerContext.Default.CoursewareDraftEntityIdentities);
    }

    private static IReadOnlyList<CoursewareDraftEntityIdentity> DeserializeEntityIdentities(ReadOnlySpan<byte> json)
    {
        return JsonSerializer.Deserialize(
                json,
                CoursewareDesignJsonSerializerContext.Default.CoursewareDraftEntityIdentities)
            ?? throw new InvalidOperationException("The stored draft entity identities could not be deserialized.");
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}

internal static class CoursewareDraftEntityIdentityFactory
{
    public static void ValidateStableIds(CoursewareDesignSystem designSystem)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        Validate(CoursewareDraftEntityKind.SpacingToken, designSystem.Spacing.Tokens.Select(item => item.TokenId));
        Validate(CoursewareDraftEntityKind.TypographyToken, designSystem.Typography.Tokens.Select(item => item.TokenId));
        Validate(CoursewareDraftEntityKind.ColorToken, designSystem.Colors.Tokens.Select(item => item.TokenId));
        Validate(CoursewareDraftEntityKind.EffectToken, designSystem.Effects.Tokens.Select(item => item.TokenId));
        Validate(CoursewareDraftEntityKind.Component, designSystem.Components.Select(item => item.ComponentId));
        Validate(CoursewareDraftEntityKind.PageType, designSystem.PageTypes.Select(item => item.PageTypeId));
        Validate(CoursewareDraftEntityKind.Assignment, designSystem.PageTypeAssignments.Select(item => item.SlideId));
        Validate(CoursewareDraftEntityKind.Template, designSystem.PageTemplates.Select(item => item.TemplateId));
    }

    public static IReadOnlyList<CoursewareDraftEntityIdentity> Create(CoursewareDesignSystem designSystem)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        var identities = new List<CoursewareDraftEntityIdentity>(
            designSystem.Spacing.Tokens.Count
            + designSystem.Typography.Tokens.Count
            + designSystem.Colors.Tokens.Count
            + designSystem.Effects.Tokens.Count
            + designSystem.Components.Count
            + designSystem.PageTypes.Count
            + designSystem.PageTypeAssignments.Count
            + designSystem.PageTemplates.Count);

        Add(identities, CoursewareDraftEntityKind.SpacingToken, designSystem.Spacing.Tokens.Select(item => item.TokenId));
        Add(identities, CoursewareDraftEntityKind.TypographyToken, designSystem.Typography.Tokens.Select(item => item.TokenId));
        Add(identities, CoursewareDraftEntityKind.ColorToken, designSystem.Colors.Tokens.Select(item => item.TokenId));
        Add(identities, CoursewareDraftEntityKind.EffectToken, designSystem.Effects.Tokens.Select(item => item.TokenId));
        Add(identities, CoursewareDraftEntityKind.Component, designSystem.Components.Select(item => item.ComponentId));
        Add(identities, CoursewareDraftEntityKind.PageType, designSystem.PageTypes.Select(item => item.PageTypeId));
        Add(identities, CoursewareDraftEntityKind.Assignment, designSystem.PageTypeAssignments.Select(item => item.SlideId));
        Add(identities, CoursewareDraftEntityKind.Template, designSystem.PageTemplates.Select(item => item.TemplateId));
        return identities;
    }

    private static void Add(
        ICollection<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftEntityKind kind,
        IEnumerable<string> stableIds)
    {
        foreach (var stableId in stableIds)
        {
            identities.Add(new CoursewareDraftEntityIdentity
            {
                Kind = kind,
                EntityKey = CoursewareDraftEntityKey.Create().Value,
                StableId = stableId,
            });
        }
    }

    private static void Validate(CoursewareDraftEntityKind kind, IEnumerable<string> stableIds)
    {
        var ids = stableIds.ToArray();
        if (ids.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException($"{kind} StableId cannot be null or whitespace.", nameof(stableIds));
        }

        var duplicate = ids.GroupBy(id => id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"{kind} StableId must be unique: {duplicate.Key}", nameof(stableIds));
        }
    }
}
