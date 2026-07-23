using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Applies authorized draft operations atomically and commits immutable revisions.
/// </summary>
public sealed class CoursewareDraftMutationService
{
    private readonly ICoursewareDesignDraftStore _store;

    /// <summary>
    /// Initializes a draft mutation service.
    /// </summary>
    public CoursewareDraftMutationService(ICoursewareDesignDraftStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <summary>Reads the manifest of the latest draft revision.</summary>
    public async ValueTask<CoursewareDraftManifest?> GetManifestAsync(
        string draftId,
        CancellationToken cancellationToken = default)
    {
        var revision = await _store.GetLatestAsync(draftId, cancellationToken).ConfigureAwait(false);
        return revision is null ? null : new CoursewareDraftAggregate(revision).GetManifest();
    }

    /// <summary>Reads one section from the latest draft revision.</summary>
    public async ValueTask<CoursewareDraftSectionSnapshot?> ReadSectionAsync(
        string draftId,
        CoursewareDraftEntityKind entityKind,
        CancellationToken cancellationToken = default)
    {
        var revision = await _store.GetLatestAsync(draftId, cancellationToken).ConfigureAwait(false);
        return revision is null ? null : new CoursewareDraftAggregate(revision).ReadSection(entityKind);
    }

    /// <summary>Reads one entity from the latest draft revision.</summary>
    public async ValueTask<CoursewareDraftSectionSnapshot?> ReadEntityAsync(
        string draftId,
        CoursewareDraftEntityKey entityKey,
        CancellationToken cancellationToken = default)
    {
        var revision = await _store.GetLatestAsync(draftId, cancellationToken).ConfigureAwait(false);
        return revision is null ? null : new CoursewareDraftAggregate(revision).ReadEntity(entityKey);
    }

    /// <summary>Applies one atomic mutation request.</summary>
    public async ValueTask<CoursewareDraftMutationResult> MutateAsync(
        CoursewareDraftMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);
        var revisions = await _store.GetRevisionsAsync(request.DraftId, cancellationToken).ConfigureAwait(false);
        var current = revisions.SingleOrDefault(revision => revision.Revision == request.ExpectedRevision);
        if (current is null)
        {
            throw new CoursewareDesignDraftConcurrencyException(
                request.DraftId,
                request.ExpectedRevision,
                revisions.Count == 0 ? null : revisions[^1].Revision);
        }

        var actualRevision = revisions[^1].Revision;

        var designSystem = current.DesignSystem;
        var identities = current.EntityIdentities.ToList();
        var createdEntityKeys = new List<CoursewareDraftEntityKey>(request.Operations.Count);
        for (var operationIndex = 0; operationIndex < request.Operations.Count; operationIndex++)
        {
            var diagnostic = ApplyOperation(
                designSystem,
                identities,
                request.Operations[operationIndex],
                request.OperationId,
                operationIndex,
                createdEntityKeys);
            if (diagnostic is not null)
            {
                ThrowIfStale(request, actualRevision);
                return Failure(current, diagnostic);
            }
        }

        var validationDiagnostics = ValidateDraftReferences(designSystem);
        if (validationDiagnostics.Count > 0)
        {
            ThrowIfStale(request, actualRevision);
            return Failure(current, validationDiagnostics);
        }

        if (current.HasSamePayload(designSystem, identities))
        {
            var noOp = await _store.RecordNoOpAsync(
                    request.OperationId,
                    request.ExpectedRevision,
                    current,
                    cancellationToken)
                .ConfigureAwait(false);
            return new CoursewareDraftMutationResult
            {
                Success = true,
                WasNoOp = true,
                Revision = noOp.Revision,
            };
        }

        var next = current.CreateNext(designSystem, identities, request.CreatedAt);
        var commit = await _store.CommitAsync(
                request.OperationId,
                request.ExpectedRevision,
                next,
                cancellationToken)
            .ConfigureAwait(false);
        return new CoursewareDraftMutationResult
        {
            Success = true,
            WasCommitted = commit.WasCommitted,
            Revision = commit.Revision,
            CreatedEntityKeys = createdEntityKeys,
        };
    }

    /// <summary>Creates a candidate proposal from the latest immutable revision without publishing it.</summary>
    public async ValueTask<CoursewareDraftCandidateProposal> ProposeCandidateAsync(
        string draftId,
        long expectedRevision,
        string candidateId,
        string inputFingerprint,
        string qualityPolicyVersion,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var revision = await _store.GetLatestAsync(draftId, cancellationToken).ConfigureAwait(false)
            ?? throw new CoursewareDesignDraftConcurrencyException(draftId, expectedRevision, actualRevision: null);
        if (revision.Revision != expectedRevision)
        {
            throw new CoursewareDesignDraftConcurrencyException(draftId, expectedRevision, revision.Revision);
        }

        return new CoursewareDraftCandidateProposal
        {
            Candidate = CoursewareCandidate.Create(candidateId, inputFingerprint, qualityPolicyVersion, revision, createdAt),
            Manifest = new CoursewareDraftAggregate(revision).GetManifest(),
        };
    }

    private static CoursewareValidationDiagnostic? ApplyOperation(
        CoursewareDesignSystem designSystem,
        List<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftOperation operation,
        string operationId,
        int operationIndex,
        ICollection<CoursewareDraftEntityKey> createdEntityKeys)
    {
        return operation.OperationKind switch
        {
            CoursewareDraftOperationKind.Add => Add(designSystem, identities, operation, operationId, operationIndex, createdEntityKeys),
            CoursewareDraftOperationKind.Replace => Replace(designSystem, identities, operation),
            CoursewareDraftOperationKind.Delete => Delete(designSystem, identities, operation),
            CoursewareDraftOperationKind.RenameStableId => RenameStableId(designSystem, identities, operation),
            _ => Diagnostic("UnsupportedOperation", "operations", "不支持的草稿操作。"),
        };
    }

    private static CoursewareValidationDiagnostic? Add(
        CoursewareDesignSystem designSystem,
        List<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftOperation operation,
        string operationId,
        int operationIndex,
        ICollection<CoursewareDraftEntityKey> createdEntityKeys)
    {
        if (operation.EntityKey is not null)
        {
            return Diagnostic("EntityKeyNotAllowed", "operations.entityKey", "新增实体时 EntityKey 必须由宿主生成。");
        }

        if (operation.Payload is null || !CoursewareDraftAggregate.HasExactlyOnePayload(operation.Payload, operation.EntityKind))
        {
            return Diagnostic("InvalidEntityPayload", "operations.payload", "实体载荷必须与 EntityKind 匹配且 StableId 不能为空。");
        }

        var stableId = CoursewareDraftAggregate.GetStableId(operation.Payload, operation.EntityKind);
        if (identities.Any(identity => identity.Kind == operation.EntityKind
            && string.Equals(identity.StableId, stableId, StringComparison.Ordinal)))
        {
            return Diagnostic("DuplicateStableId", "operations.payload", $"StableId 已存在：{stableId}");
        }

        AddPayload(designSystem, operation.EntityKind, operation.Payload);
        var entityKey = CoursewareDraftEntityKey.Create(operationId, operationIndex);
        identities.Add(new CoursewareDraftEntityIdentity
        {
            Kind = operation.EntityKind,
            EntityKey = entityKey.Value,
            StableId = stableId,
        });
        createdEntityKeys.Add(entityKey);
        return null;
    }

    private static CoursewareValidationDiagnostic? Replace(
        CoursewareDesignSystem designSystem,
        List<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftOperation operation)
    {
        if (!TryGetIdentity(identities, operation, out var identity, out var diagnostic))
        {
            return diagnostic;
        }

        if (operation.Payload is null || !CoursewareDraftAggregate.HasExactlyOnePayload(operation.Payload, operation.EntityKind))
        {
            return Diagnostic("InvalidEntityPayload", "operations.payload", "实体载荷必须与 EntityKind 匹配且 StableId 不能为空。");
        }

        var replacementStableId = CoursewareDraftAggregate.GetStableId(operation.Payload, operation.EntityKind);
        if (!string.Equals(identity.StableId, replacementStableId, StringComparison.Ordinal))
        {
            return Diagnostic("StableIdChangeRequiresRename", "operations.payload", "替换操作不得修改 StableId，请使用原子重命名操作。");
        }

        ReplacePayload(designSystem, identity, operation.Payload);
        return null;
    }

    private static CoursewareValidationDiagnostic? Delete(
        CoursewareDesignSystem designSystem,
        List<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftOperation operation)
    {
        if (!TryGetIdentity(identities, operation, out var identity, out var diagnostic))
        {
            return diagnostic;
        }

        RemovePayload(designSystem, identity);
        identities.Remove(identity);
        return null;
    }

    private static CoursewareValidationDiagnostic? RenameStableId(
        CoursewareDesignSystem designSystem,
        List<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftOperation operation)
    {
        if (!TryGetIdentity(identities, operation, out var identity, out var diagnostic))
        {
            return diagnostic;
        }

        if (string.IsNullOrWhiteSpace(operation.NewStableId))
        {
            return Diagnostic("StableIdRequired", "operations.newStableId", "新的 StableId 不能为空。");
        }

        if (identity.Kind == CoursewareDraftEntityKind.Assignment)
        {
            return Diagnostic("AssignmentStableIdImmutable", "operations.newStableId", "Assignment 的 SlideId 来自不可变输入，不能通过草稿操作重命名。");
        }

        if (identities.Any(candidate => candidate.Kind == identity.Kind
            && !string.Equals(candidate.EntityKey, identity.EntityKey, StringComparison.Ordinal)
            && string.Equals(candidate.StableId, operation.NewStableId, StringComparison.Ordinal)))
        {
            return Diagnostic("DuplicateStableId", "operations.newStableId", $"StableId 已存在：{operation.NewStableId}");
        }

        RenamePayloadAndReferences(designSystem, identity, operation.NewStableId);
        identities[identities.IndexOf(identity)] = identity with { StableId = operation.NewStableId };
        return null;
    }

    private static bool TryGetIdentity(
        IReadOnlyList<CoursewareDraftEntityIdentity> identities,
        CoursewareDraftOperation operation,
        out CoursewareDraftEntityIdentity identity,
        out CoursewareValidationDiagnostic? diagnostic)
    {
        identity = null!;
        diagnostic = null;
        if (operation.EntityKey is null)
        {
            diagnostic = Diagnostic("EntityKeyRequired", "operations.entityKey", "该操作必须提供 EntityKey。");
            return false;
        }

        identity = identities.SingleOrDefault(candidate => string.Equals(
            candidate.EntityKey,
            operation.EntityKey.Value.Value,
            StringComparison.Ordinal))!;
        if (identity is null)
        {
            diagnostic = Diagnostic("UnknownEntityKey", "operations.entityKey", $"未知 EntityKey：{operation.EntityKey}");
            return false;
        }

        if (identity.Kind != operation.EntityKind)
        {
            diagnostic = Diagnostic("EntityKindMismatch", "operations.entityKind", "EntityKind 与 EntityKey 指向的实体不一致。");
            return false;
        }

        return true;
    }

    private static void AddPayload(
        CoursewareDesignSystem designSystem,
        CoursewareDraftEntityKind kind,
        CoursewareDraftEntityPayload payload)
    {
        switch (kind)
        {
            case CoursewareDraftEntityKind.SpacingToken:
                designSystem.Spacing = designSystem.Spacing with { Tokens = Append(designSystem.Spacing.Tokens, payload.SpacingToken!) };
                break;
            case CoursewareDraftEntityKind.TypographyToken:
                designSystem.Typography = designSystem.Typography with { Tokens = Append(designSystem.Typography.Tokens, payload.TypographyToken!) };
                break;
            case CoursewareDraftEntityKind.ColorToken:
                designSystem.Colors = designSystem.Colors with { Tokens = Append(designSystem.Colors.Tokens, payload.ColorToken!) };
                break;
            case CoursewareDraftEntityKind.EffectToken:
                designSystem.Effects = designSystem.Effects with { Tokens = Append(designSystem.Effects.Tokens, payload.EffectToken!) };
                break;
            case CoursewareDraftEntityKind.Component:
                designSystem.Components = Append(designSystem.Components, payload.Component!);
                break;
            case CoursewareDraftEntityKind.PageType:
                designSystem.PageTypes = Append(designSystem.PageTypes, payload.PageType!);
                break;
            case CoursewareDraftEntityKind.Assignment:
                designSystem.PageTypeAssignments = Append(designSystem.PageTypeAssignments, payload.Assignment!);
                break;
            case CoursewareDraftEntityKind.Template:
                designSystem.PageTemplates = Append(designSystem.PageTemplates, payload.Template!);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported draft entity kind.");
        }
    }

    private static void ReplacePayload(
        CoursewareDesignSystem designSystem,
        CoursewareDraftEntityIdentity identity,
        CoursewareDraftEntityPayload payload)
    {
        switch (identity.Kind)
        {
            case CoursewareDraftEntityKind.SpacingToken:
                designSystem.Spacing = designSystem.Spacing with { Tokens = Replace(designSystem.Spacing.Tokens, item => item.TokenId, identity.StableId, payload.SpacingToken!) };
                break;
            case CoursewareDraftEntityKind.TypographyToken:
                designSystem.Typography = designSystem.Typography with { Tokens = Replace(designSystem.Typography.Tokens, item => item.TokenId, identity.StableId, payload.TypographyToken!) };
                break;
            case CoursewareDraftEntityKind.ColorToken:
                designSystem.Colors = designSystem.Colors with { Tokens = Replace(designSystem.Colors.Tokens, item => item.TokenId, identity.StableId, payload.ColorToken!) };
                break;
            case CoursewareDraftEntityKind.EffectToken:
                designSystem.Effects = designSystem.Effects with { Tokens = Replace(designSystem.Effects.Tokens, item => item.TokenId, identity.StableId, payload.EffectToken!) };
                break;
            case CoursewareDraftEntityKind.Component:
                designSystem.Components = Replace(designSystem.Components, item => item.ComponentId, identity.StableId, payload.Component!);
                break;
            case CoursewareDraftEntityKind.PageType:
                designSystem.PageTypes = Replace(designSystem.PageTypes, item => item.PageTypeId, identity.StableId, payload.PageType!);
                break;
            case CoursewareDraftEntityKind.Assignment:
                designSystem.PageTypeAssignments = Replace(designSystem.PageTypeAssignments, item => item.SlideId, identity.StableId, payload.Assignment!);
                break;
            case CoursewareDraftEntityKind.Template:
                designSystem.PageTemplates = Replace(designSystem.PageTemplates, item => item.TemplateId, identity.StableId, payload.Template!);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(identity), identity.Kind, "Unsupported draft entity kind.");
        }
    }

    private static void RemovePayload(CoursewareDesignSystem designSystem, CoursewareDraftEntityIdentity identity)
    {
        switch (identity.Kind)
        {
            case CoursewareDraftEntityKind.SpacingToken:
                designSystem.Spacing = designSystem.Spacing with { Tokens = Remove(designSystem.Spacing.Tokens, item => item.TokenId, identity.StableId) };
                break;
            case CoursewareDraftEntityKind.TypographyToken:
                designSystem.Typography = designSystem.Typography with { Tokens = Remove(designSystem.Typography.Tokens, item => item.TokenId, identity.StableId) };
                break;
            case CoursewareDraftEntityKind.ColorToken:
                designSystem.Colors = designSystem.Colors with { Tokens = Remove(designSystem.Colors.Tokens, item => item.TokenId, identity.StableId) };
                break;
            case CoursewareDraftEntityKind.EffectToken:
                designSystem.Effects = designSystem.Effects with { Tokens = Remove(designSystem.Effects.Tokens, item => item.TokenId, identity.StableId) };
                break;
            case CoursewareDraftEntityKind.Component:
                designSystem.Components = Remove(designSystem.Components, item => item.ComponentId, identity.StableId);
                break;
            case CoursewareDraftEntityKind.PageType:
                designSystem.PageTypes = Remove(designSystem.PageTypes, item => item.PageTypeId, identity.StableId);
                break;
            case CoursewareDraftEntityKind.Assignment:
                designSystem.PageTypeAssignments = Remove(designSystem.PageTypeAssignments, item => item.SlideId, identity.StableId);
                break;
            case CoursewareDraftEntityKind.Template:
                designSystem.PageTemplates = Remove(designSystem.PageTemplates, item => item.TemplateId, identity.StableId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(identity), identity.Kind, "Unsupported draft entity kind.");
        }
    }

    private static void RenamePayloadAndReferences(
        CoursewareDesignSystem designSystem,
        CoursewareDraftEntityIdentity identity,
        string newStableId)
    {
        var oldStableId = identity.StableId;
        switch (identity.Kind)
        {
            case CoursewareDraftEntityKind.SpacingToken:
            case CoursewareDraftEntityKind.TypographyToken:
            case CoursewareDraftEntityKind.ColorToken:
            case CoursewareDraftEntityKind.EffectToken:
                RenameToken(designSystem, identity, newStableId);
                designSystem.Components = designSystem.Components
                    .Select(component => component with { TokenIds = ReplaceReferences(component.TokenIds, oldStableId, newStableId) })
                    .ToArray();
                designSystem.Colors = designSystem.Colors with
                {
                    Tokens = designSystem.Colors.Tokens
                        .Select(token => token with { AllowedBackgroundTokenIds = ReplaceReferences(token.AllowedBackgroundTokenIds, oldStableId, newStableId) })
                        .ToArray(),
                };
                break;
            case CoursewareDraftEntityKind.Component:
                designSystem.Components = designSystem.Components
                    .Select(component => string.Equals(component.ComponentId, oldStableId, StringComparison.Ordinal)
                        ? component with { ComponentId = newStableId }
                        : component)
                    .ToArray();
                designSystem.PageTypes = designSystem.PageTypes
                    .Select(pageType => pageType with { ComponentIds = ReplaceReferences(pageType.ComponentIds, oldStableId, newStableId) })
                    .ToArray();
                break;
            case CoursewareDraftEntityKind.PageType:
                designSystem.PageTypes = designSystem.PageTypes
                    .Select(pageType => pageType with
                    {
                        PageTypeId = string.Equals(pageType.PageTypeId, oldStableId, StringComparison.Ordinal) ? newStableId : pageType.PageTypeId,
                        FallbackPageTypeId = ReplaceReference(pageType.FallbackPageTypeId, oldStableId, newStableId),
                    })
                    .ToArray();
                designSystem.Typography = designSystem.Typography with
                {
                    Tokens = designSystem.Typography.Tokens
                        .Select(token => token with { AllowedPageTypeIds = ReplaceReferences(token.AllowedPageTypeIds, oldStableId, newStableId) })
                        .ToArray(),
                };
                designSystem.PageTypeAssignments = designSystem.PageTypeAssignments
                    .Select(assignment => assignment with { PageTypeId = ReplaceReference(assignment.PageTypeId, oldStableId, newStableId)! })
                    .ToArray();
                designSystem.PageTemplates = designSystem.PageTemplates
                    .Select(template => template with { PageTypeId = ReplaceReference(template.PageTypeId, oldStableId, newStableId)! })
                    .ToArray();
                break;
            case CoursewareDraftEntityKind.Assignment:
                designSystem.PageTypeAssignments = designSystem.PageTypeAssignments
                    .Select(assignment => string.Equals(assignment.SlideId, oldStableId, StringComparison.Ordinal)
                        ? assignment with { SlideId = newStableId }
                        : assignment)
                    .ToArray();
                break;
            case CoursewareDraftEntityKind.Template:
                designSystem.PageTemplates = designSystem.PageTemplates
                    .Select(template => template with
                    {
                        TemplateId = string.Equals(template.TemplateId, oldStableId, StringComparison.Ordinal) ? newStableId : template.TemplateId,
                        FallbackTemplateId = ReplaceReference(template.FallbackTemplateId, oldStableId, newStableId),
                    })
                    .ToArray();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(identity), identity.Kind, "Unsupported draft entity kind.");
        }
    }

    private static void RenameToken(
        CoursewareDesignSystem designSystem,
        CoursewareDraftEntityIdentity identity,
        string newStableId)
    {
        switch (identity.Kind)
        {
            case CoursewareDraftEntityKind.SpacingToken:
                designSystem.Spacing = designSystem.Spacing with
                {
                    Tokens = designSystem.Spacing.Tokens.Select(token => string.Equals(token.TokenId, identity.StableId, StringComparison.Ordinal) ? token with { TokenId = newStableId } : token).ToArray(),
                };
                break;
            case CoursewareDraftEntityKind.TypographyToken:
                designSystem.Typography = designSystem.Typography with
                {
                    Tokens = designSystem.Typography.Tokens.Select(token => string.Equals(token.TokenId, identity.StableId, StringComparison.Ordinal) ? token with { TokenId = newStableId } : token).ToArray(),
                };
                break;
            case CoursewareDraftEntityKind.ColorToken:
                designSystem.Colors = designSystem.Colors with
                {
                    Tokens = designSystem.Colors.Tokens.Select(token => string.Equals(token.TokenId, identity.StableId, StringComparison.Ordinal) ? token with { TokenId = newStableId } : token).ToArray(),
                };
                break;
            case CoursewareDraftEntityKind.EffectToken:
                designSystem.Effects = designSystem.Effects with
                {
                    Tokens = designSystem.Effects.Tokens.Select(token => string.Equals(token.TokenId, identity.StableId, StringComparison.Ordinal) ? token with { TokenId = newStableId } : token).ToArray(),
                };
                break;
        }
    }

    private static IReadOnlyList<CoursewareValidationDiagnostic> ValidateDraftReferences(CoursewareDesignSystem designSystem)
    {
        var diagnostics = new List<CoursewareValidationDiagnostic>();
        var tokenIds = designSystem.Spacing.Tokens.Select(item => item.TokenId)
            .Concat(designSystem.Typography.Tokens.Select(item => item.TokenId))
            .Concat(designSystem.Colors.Tokens.Select(item => item.TokenId))
            .Concat(designSystem.Effects.Tokens.Select(item => item.TokenId))
            .ToArray();
        ValidateUniqueAndRequired(tokenIds, "tokens", diagnostics);
        var tokenSet = tokenIds.ToHashSet(StringComparer.Ordinal);
        foreach (var component in designSystem.Components)
        {
            AddUnknownReferences(component.TokenIds, tokenSet, "UnknownTokenReference", $"components[{component.ComponentId}].tokenIds", diagnostics);
        }

        foreach (var color in designSystem.Colors.Tokens)
        {
            AddUnknownReferences(color.AllowedBackgroundTokenIds, tokenSet, "UnknownTokenReference", $"colors.tokens[{color.TokenId}].allowedBackgroundTokenIds", diagnostics);
        }

        var componentIds = designSystem.Components.Select(item => item.ComponentId).ToArray();
        ValidateUniqueAndRequired(componentIds, "components", diagnostics);
        var componentSet = componentIds.ToHashSet(StringComparer.Ordinal);
        var pageTypeIds = designSystem.PageTypes.Select(item => item.PageTypeId).ToArray();
        ValidateUniqueAndRequired(pageTypeIds, "pageTypes", diagnostics);
        var pageTypeSet = pageTypeIds.ToHashSet(StringComparer.Ordinal);
        foreach (var typography in designSystem.Typography.Tokens)
        {
            AddUnknownReferences(typography.AllowedPageTypeIds, pageTypeSet, "UnknownPageTypeReference", $"typography.tokens[{typography.TokenId}].allowedPageTypeIds", diagnostics);
        }

        foreach (var pageType in designSystem.PageTypes)
        {
            AddUnknownReferences(pageType.ComponentIds, componentSet, "UnknownComponentReference", $"pageTypes[{pageType.PageTypeId}].componentIds", diagnostics);
            AddUnknownReference(pageType.FallbackPageTypeId, pageTypeSet, "UnknownPageTypeReference", $"pageTypes[{pageType.PageTypeId}].fallbackPageTypeId", diagnostics);
        }

        var assignmentIds = designSystem.PageTypeAssignments.Select(item => item.SlideId).ToArray();
        ValidateUniqueAndRequired(assignmentIds, "pageTypeAssignments", diagnostics);
        foreach (var assignment in designSystem.PageTypeAssignments)
        {
            AddUnknownReference(assignment.PageTypeId, pageTypeSet, "UnknownPageTypeReference", $"pageTypeAssignments[{assignment.SlideId}].pageTypeId", diagnostics);
        }

        var templateIds = designSystem.PageTemplates.Select(item => item.TemplateId).ToArray();
        ValidateUniqueAndRequired(templateIds, "pageTemplates", diagnostics);
        var templateSet = templateIds.ToHashSet(StringComparer.Ordinal);
        foreach (var template in designSystem.PageTemplates)
        {
            AddUnknownReference(template.PageTypeId, pageTypeSet, "UnknownPageTypeReference", $"pageTemplates[{template.TemplateId}].pageTypeId", diagnostics);
            AddUnknownReference(template.FallbackTemplateId, templateSet, "UnknownTemplateReference", $"pageTemplates[{template.TemplateId}].fallbackTemplateId", diagnostics);
        }

        return diagnostics;
    }

    private static void ValidateRequest(CoursewareDraftMutationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            throw new ArgumentException("OperationId cannot be null or whitespace.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DraftId))
        {
            throw new ArgumentException("DraftId cannot be null or whitespace.", nameof(request));
        }

        if (request.ExpectedRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.ExpectedRevision, "Expected revision cannot be negative.");
        }

        if (request.Operations.Count == 0)
        {
            throw new ArgumentException("At least one draft operation is required.", nameof(request));
        }

        ValidateScope(request);
    }

    private static void ThrowIfStale(CoursewareDraftMutationRequest request, long actualRevision)
    {
        if (actualRevision != request.ExpectedRevision)
        {
            throw new CoursewareDesignDraftConcurrencyException(request.DraftId, request.ExpectedRevision, actualRevision);
        }
    }

    private static void ValidateScope(CoursewareDraftMutationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Scope.WorkItemId))
        {
            throw new ArgumentException("WorkItemId cannot be null or whitespace.", nameof(request));
        }

        foreach (var operation in request.Operations)
        {
            if (!request.Scope.Permissions.AllowedEntityKinds.Contains(operation.EntityKind)
                || !request.Scope.Permissions.AllowedOperationKinds.Contains(operation.OperationKind))
            {
                throw new CoursewareDraftScopeViolationException(request.Scope.WorkItemId, operation.EntityKind, operation.OperationKind);
            }

            if (operation.EntityKey is not null
                && request.Scope.Permissions.AllowedEntityKeys.Count > 0
                && !request.Scope.Permissions.AllowedEntityKeys.Contains(operation.EntityKey.Value.Value))
            {
                throw new CoursewareDraftScopeViolationException(request.Scope.WorkItemId, operation.EntityKind, operation.OperationKind);
            }

            if (operation.OperationKind == CoursewareDraftOperationKind.RenameStableId
                && (request.Scope.Permissions.AllowedEntityKeys.Count > 0
                    || request.Scope.Permissions.AllowedEntityKinds.Count != Enum.GetValues<CoursewareDraftEntityKind>().Length))
            {
                throw new CoursewareDraftScopeViolationException(request.Scope.WorkItemId, operation.EntityKind, operation.OperationKind);
            }
        }
    }

    private static IReadOnlyList<T> Replace<T>(
        IReadOnlyList<T> items,
        Func<T, string> keySelector,
        string stableId,
        T replacement)
    {
        return items.Select(item => string.Equals(keySelector(item), stableId, StringComparison.Ordinal) ? replacement : item).ToArray();
    }

    private static CoursewareDraftMutationResult Failure(
        CoursewareDesignDraftRevision revision,
        CoursewareValidationDiagnostic diagnostic)
    {
        return Failure(revision, [diagnostic]);
    }

    private static CoursewareDraftMutationResult Failure(
        CoursewareDesignDraftRevision revision,
        IReadOnlyList<CoursewareValidationDiagnostic> diagnostics)
    {
        return new CoursewareDraftMutationResult
        {
            Success = false,
            Revision = revision,
            Diagnostics = diagnostics,
        };
    }

    private static CoursewareValidationDiagnostic Diagnostic(string code, string path, string message)
    {
        return new CoursewareValidationDiagnostic
        {
            Code = code,
            Path = path,
            Message = message,
            Severity = CoursewareValidationSeverity.Error,
        };
    }

    private static void ValidateUniqueAndRequired(
        IReadOnlyList<string> stableIds,
        string path,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        if (stableIds.Any(string.IsNullOrWhiteSpace))
        {
            diagnostics.Add(Diagnostic("StableIdRequired", path, "StableId 不能为空。"));
        }

        foreach (var duplicate in stableIds.Where(id => !string.IsNullOrWhiteSpace(id)).GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(Diagnostic("DuplicateStableId", path, $"StableId 重复：{duplicate.Key}"));
        }
    }

    private static void AddUnknownReferences(
        IEnumerable<string> references,
        IReadOnlySet<string> knownIds,
        string code,
        string path,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        foreach (var reference in references.Where(reference => !knownIds.Contains(reference)).Distinct(StringComparer.Ordinal))
        {
            diagnostics.Add(Diagnostic(code, path, $"引用了未知 StableId：{reference}"));
        }
    }

    private static void AddUnknownReference(
        string? reference,
        IReadOnlySet<string> knownIds,
        string code,
        string path,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(reference) && !knownIds.Contains(reference))
        {
            diagnostics.Add(Diagnostic(code, path, $"引用了未知 StableId：{reference}"));
        }
    }

    private static IReadOnlyList<T> Append<T>(IReadOnlyList<T> items, T item) => items.Append(item).ToArray();

    private static IReadOnlyList<T> Remove<T>(IReadOnlyList<T> items, Func<T, string> keySelector, string stableId)
    {
        return items.Where(item => !string.Equals(keySelector(item), stableId, StringComparison.Ordinal)).ToArray();
    }

    private static IReadOnlyList<string> ReplaceReferences(
        IReadOnlyList<string> references,
        string oldStableId,
        string newStableId)
    {
        return references.Select(reference => ReplaceReference(reference, oldStableId, newStableId)!).ToArray();
    }

    private static string? ReplaceReference(string? reference, string oldStableId, string newStableId)
    {
        return string.Equals(reference, oldStableId, StringComparison.Ordinal) ? newStableId : reference;
    }
}

/// <summary>
/// Indicates that a draft operation exceeded its repair work-item scope.
/// </summary>
public sealed class CoursewareDraftScopeViolationException : InvalidOperationException
{
    /// <summary>
    /// Initializes a scope violation exception.
    /// </summary>
    public CoursewareDraftScopeViolationException(
        string workItemId,
        CoursewareDraftEntityKind entityKind,
        CoursewareDraftOperationKind operationKind)
        : base($"Work item '{workItemId}' cannot perform {operationKind} on {entityKind}.")
    {
        WorkItemId = workItemId;
        EntityKind = entityKind;
        OperationKind = operationKind;
    }

    /// <summary>Gets the work-item identifier.</summary>
    public string WorkItemId { get; }

    /// <summary>Gets the rejected entity kind.</summary>
    public CoursewareDraftEntityKind EntityKind { get; }

    /// <summary>Gets the rejected operation kind.</summary>
    public CoursewareDraftOperationKind OperationKind { get; }
}
