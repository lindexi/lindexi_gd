using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Provides stable host addressing and isolated reads for one immutable draft revision.
/// </summary>
public sealed class CoursewareDraftAggregate
{
    private readonly CoursewareDesignDraftRevision _revision;
    private readonly IReadOnlyDictionary<string, CoursewareDraftEntityIdentity> _identitiesByKey;

    /// <summary>
    /// Rehydrates an aggregate from an immutable revision.
    /// </summary>
    public CoursewareDraftAggregate(CoursewareDesignDraftRevision revision)
    {
        ArgumentNullException.ThrowIfNull(revision);
        _revision = revision;
        var identities = revision.EntityIdentities;
        if (identities.Any(identity => string.IsNullOrWhiteSpace(identity.EntityKey) || string.IsNullOrWhiteSpace(identity.StableId)))
        {
            throw new InvalidOperationException("Draft entity identities must contain non-empty entity keys and stable identifiers.");
        }

        if (identities.GroupBy(identity => identity.EntityKey, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("Draft entity keys must be unique.");
        }

        _identitiesByKey = identities.ToDictionary(identity => identity.EntityKey, StringComparer.Ordinal);
        ValidateIdentityCoverage(revision.DesignSystem, identities);
    }

    /// <summary>Gets the immutable source revision.</summary>
    public CoursewareDesignDraftRevision Revision => _revision;

    /// <summary>Gets a manifest of all addressable entities.</summary>
    public CoursewareDraftManifest GetManifest()
    {
        return new CoursewareDraftManifest
        {
            DraftId = _revision.DraftId,
            Revision = _revision.Revision,
            RevisionFingerprint = _revision.RevisionFingerprint,
            Entities = _identitiesByKey.Values
                .OrderBy(identity => identity.Kind)
                .ThenBy(identity => identity.StableId, StringComparer.Ordinal)
                .Select(identity => new CoursewareDraftManifestEntry
                {
                    Kind = identity.Kind,
                    EntityKey = CoursewareDraftEntityKey.Parse(identity.EntityKey),
                    StableId = identity.StableId,
                })
                .ToArray(),
        };
    }

    /// <summary>Reads all entities in one addressable section.</summary>
    public CoursewareDraftSectionSnapshot ReadSection(CoursewareDraftEntityKind entityKind)
    {
        var designSystem = _revision.DesignSystem;
        return new CoursewareDraftSectionSnapshot
        {
            DraftId = _revision.DraftId,
            Revision = _revision.Revision,
            EntityKind = entityKind,
            Entities = GetEntities(designSystem, entityKind).ToArray(),
        };
    }

    /// <summary>Reads one entity by its host-generated key.</summary>
    public CoursewareDraftSectionSnapshot ReadEntity(CoursewareDraftEntityKey entityKey)
    {
        if (!_identitiesByKey.TryGetValue(entityKey.Value, out var identity))
        {
            throw new KeyNotFoundException($"Draft entity '{entityKey}' does not exist.");
        }

        var designSystem = _revision.DesignSystem;
        var payload = FindEntity(designSystem, identity)
            ?? throw new InvalidOperationException($"Draft entity '{entityKey}' is missing from the design-system payload.");
        return new CoursewareDraftSectionSnapshot
        {
            DraftId = _revision.DraftId,
            Revision = _revision.Revision,
            EntityKind = identity.Kind,
            EntityKey = entityKey,
            Entities = [payload],
        };
    }

    internal bool TryGetIdentity(CoursewareDraftEntityKey entityKey, out CoursewareDraftEntityIdentity identity)
    {
        return _identitiesByKey.TryGetValue(entityKey.Value, out identity!);
    }

    private static IEnumerable<CoursewareDraftEntityPayload> GetEntities(
        CoursewareDesignSystem designSystem,
        CoursewareDraftEntityKind entityKind)
    {
        return entityKind switch
        {
            CoursewareDraftEntityKind.SpacingToken => designSystem.Spacing.Tokens.Select(item => new CoursewareDraftEntityPayload { SpacingToken = item }),
            CoursewareDraftEntityKind.TypographyToken => designSystem.Typography.Tokens.Select(item => new CoursewareDraftEntityPayload { TypographyToken = item }),
            CoursewareDraftEntityKind.ColorToken => designSystem.Colors.Tokens.Select(item => new CoursewareDraftEntityPayload { ColorToken = item }),
            CoursewareDraftEntityKind.EffectToken => designSystem.Effects.Tokens.Select(item => new CoursewareDraftEntityPayload { EffectToken = item }),
            CoursewareDraftEntityKind.Component => designSystem.Components.Select(item => new CoursewareDraftEntityPayload { Component = item }),
            CoursewareDraftEntityKind.PageType => designSystem.PageTypes.Select(item => new CoursewareDraftEntityPayload { PageType = item }),
            CoursewareDraftEntityKind.Assignment => designSystem.PageTypeAssignments.Select(item => new CoursewareDraftEntityPayload { Assignment = item }),
            CoursewareDraftEntityKind.Template => designSystem.PageTemplates.Select(item => new CoursewareDraftEntityPayload { Template = item }),
            _ => throw new ArgumentOutOfRangeException(nameof(entityKind), entityKind, "Unsupported draft entity kind."),
        };
    }

    private static CoursewareDraftEntityPayload? FindEntity(
        CoursewareDesignSystem designSystem,
        CoursewareDraftEntityIdentity identity)
    {
        return GetEntities(designSystem, identity.Kind)
            .SingleOrDefault(payload => string.Equals(GetStableId(payload, identity.Kind), identity.StableId, StringComparison.Ordinal));
    }

    internal static string GetStableId(CoursewareDraftEntityPayload payload, CoursewareDraftEntityKind entityKind)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return entityKind switch
        {
            CoursewareDraftEntityKind.SpacingToken => payload.SpacingToken?.TokenId ?? string.Empty,
            CoursewareDraftEntityKind.TypographyToken => payload.TypographyToken?.TokenId ?? string.Empty,
            CoursewareDraftEntityKind.ColorToken => payload.ColorToken?.TokenId ?? string.Empty,
            CoursewareDraftEntityKind.EffectToken => payload.EffectToken?.TokenId ?? string.Empty,
            CoursewareDraftEntityKind.Component => payload.Component?.ComponentId ?? string.Empty,
            CoursewareDraftEntityKind.PageType => payload.PageType?.PageTypeId ?? string.Empty,
            CoursewareDraftEntityKind.Assignment => payload.Assignment?.SlideId ?? string.Empty,
            CoursewareDraftEntityKind.Template => payload.Template?.TemplateId ?? string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(entityKind), entityKind, "Unsupported draft entity kind."),
        };
    }

    internal static bool HasExactlyOnePayload(CoursewareDraftEntityPayload payload, CoursewareDraftEntityKind entityKind)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var payloadCount = new object?[]
        {
            payload.SpacingToken,
            payload.TypographyToken,
            payload.ColorToken,
            payload.EffectToken,
            payload.Component,
            payload.PageType,
            payload.Assignment,
            payload.Template,
        }.Count(value => value is not null);
        return payloadCount == 1 && !string.IsNullOrWhiteSpace(GetStableId(payload, entityKind));
    }

    private static void ValidateIdentityCoverage(
        CoursewareDesignSystem designSystem,
        IReadOnlyList<CoursewareDraftEntityIdentity> identities)
    {
        var actual = Enum.GetValues<CoursewareDraftEntityKind>()
            .SelectMany(kind => GetEntities(designSystem, kind).Select(payload => (Kind: kind, StableId: GetStableId(payload, kind))))
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.StableId, StringComparer.Ordinal)
            .ToArray();
        var mapped = identities
            .Select(identity => (identity.Kind, identity.StableId))
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.StableId, StringComparer.Ordinal)
            .ToArray();
        if (!actual.SequenceEqual(mapped))
        {
            throw new InvalidOperationException("Draft entity identities do not match the design-system payload.");
        }
    }
}
