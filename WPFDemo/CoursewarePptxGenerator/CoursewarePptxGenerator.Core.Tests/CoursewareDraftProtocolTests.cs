using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGenerator.Core.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CoursewareDraftProtocolTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    [TestMethod(DisplayName = "EntityKey 应稳定且不依赖 StableId")]
    [Timeout(60_000)]
    public async Task EntityKeyShouldRemainStableWhenStableIdChanges()
    {
        var context = await CreateContextAsync();
        var component = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Component);

        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "rename-component",
            [Rename(CoursewareDraftEntityKind.Component, component.EntityKey, "renamed-component")]));
        var renamed = new CoursewareDraftAggregate(result.Revision).GetManifest().Entities
            .Single(item => item.Kind == CoursewareDraftEntityKind.Component);

        Assert.AreEqual(component.EntityKey, renamed.EntityKey);
        Assert.AreEqual("renamed-component", renamed.StableId);
    }

    [TestMethod(DisplayName = "空 StableId 写入失败且 Revision 不变")]
    [Timeout(60_000)]
    public async Task EmptyStableIdShouldFailWithoutChangingRevision()
    {
        var context = await CreateContextAsync();
        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "add-empty-token",
            [Add(CoursewareDraftEntityKind.SpacingToken, new CoursewareDraftEntityPayload { SpacingToken = new CoursewareSpacingToken() })]));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0L, (await context.Store.GetLatestAsync(context.Initial.DraftId))!.Revision);
    }

    [TestMethod(DisplayName = "组件写入引用未知 Token 时应立即失败")]
    [Timeout(60_000)]
    public async Task UnknownTokenReferenceShouldRejectComponentWrite()
    {
        var context = await CreateContextAsync();
        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "add-invalid-component",
            [Add(CoursewareDraftEntityKind.Component, ComponentPayload("invalid-component", "unknown-token"))]));

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(item => item.Code == "UnknownTokenReference"));
    }

    [TestMethod(DisplayName = "同一事务新增 Token 与引用组件应原子成功")]
    [Timeout(60_000)]
    public async Task TokenAndReferencingComponentShouldCommitAtomically()
    {
        var context = await CreateContextAsync();
        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "add-token-and-component",
            [
                Add(CoursewareDraftEntityKind.SpacingToken, new CoursewareDraftEntityPayload
                {
                    SpacingToken = new CoursewareSpacingToken { TokenId = "space-lg", Value = 48, Purpose = "大间距" },
                }),
                Add(CoursewareDraftEntityKind.Component, ComponentPayload("new-component", "space-lg")),
            ]));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1L, result.Revision.Revision);
        Assert.HasCount(2, result.CreatedEntityKeys);
    }

    [TestMethod(DisplayName = "事务任一操作失败时应全部回滚")]
    [Timeout(60_000)]
    public async Task FailedOperationShouldRollbackWholeTransaction()
    {
        var context = await CreateContextAsync();
        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "rollback-batch",
            [
                Add(CoursewareDraftEntityKind.SpacingToken, new CoursewareDraftEntityPayload
                {
                    SpacingToken = new CoursewareSpacingToken { TokenId = "space-lg", Value = 48, Purpose = "大间距" },
                }),
                Add(CoursewareDraftEntityKind.Component, ComponentPayload("invalid-component", "unknown-token")),
            ]));

        Assert.IsFalse(result.Success);
        Assert.IsFalse((await context.Service.GetManifestAsync(context.Initial.DraftId))!.Entities.Any(item => item.StableId == "space-lg"));
    }

    [TestMethod(DisplayName = "按 EntityKey 应可删除 StableId 异常的脏实体")]
    [Timeout(60_000)]
    public async Task EntityKeyShouldDeleteEntityWithoutStableIdLookup()
    {
        var context = await CreateContextAsync();
        var assignment = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Assignment);
        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "delete-assignment",
            [Delete(CoursewareDraftEntityKind.Assignment, assignment.EntityKey)]));

        Assert.IsTrue(result.Success);
        Assert.IsFalse(new CoursewareDraftAggregate(result.Revision).GetManifest().Entities.Any(item => item.EntityKey == assignment.EntityKey));
    }

    [TestMethod(DisplayName = "PageType 重命名后全部直接引用应同步更新")]
    [Timeout(60_000)]
    public async Task PageTypeRenameShouldUpdateAllDirectReferences()
    {
        var context = await CreateContextAsync();
        var pageType = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.PageType);
        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "rename-page-type",
            [Rename(CoursewareDraftEntityKind.PageType, pageType.EntityKey, "content-v2")]));
        var designSystem = result.Revision.DesignSystem;

        Assert.IsTrue(designSystem.Typography.Tokens.Single().AllowedPageTypeIds.Contains("content-v2"));
        Assert.AreEqual("content-v2", designSystem.PageTypeAssignments.Single().PageTypeId);
        Assert.AreEqual("content-v2", designSystem.PageTemplates.Single().PageTypeId);
    }

    [TestMethod(DisplayName = "陈旧 ExpectedRevision 应被拒绝")]
    [Timeout(60_000)]
    public async Task StaleExpectedRevisionShouldBeRejected()
    {
        var context = await CreateContextAsync();
        var assignment = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Assignment);
        await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "delete-first",
            [Delete(CoursewareDraftEntityKind.Assignment, assignment.EntityKey)]));

        await Assert.ThrowsExactlyAsync<CoursewareDesignDraftConcurrencyException>(
            () => context.Service.MutateAsync(CreateRequest(
                context.Initial,
                "delete-stale",
                [Delete(CoursewareDraftEntityKind.Assignment, assignment.EntityKey)])).AsTask());
    }

    [TestMethod(DisplayName = "陈旧 ExpectedRevision 的 no-op 也应被拒绝")]
    [Timeout(60_000)]
    public async Task StaleNoOpShouldBeRejected()
    {
        var context = await CreateContextAsync();
        var assignment = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Assignment);
        var component = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Component);
        var originalComponent = (await context.Service.ReadEntityAsync(context.Initial.DraftId, component.EntityKey))!.Entities.Single();
        await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "delete-before-stale-no-op",
            [Delete(CoursewareDraftEntityKind.Assignment, assignment.EntityKey)]));

        await Assert.ThrowsExactlyAsync<CoursewareDesignDraftConcurrencyException>(
            () => context.Service.MutateAsync(CreateRequest(
                context.Initial,
                "stale-no-op",
                [Replace(CoursewareDraftEntityKind.Component, component.EntityKey, originalComponent)])).AsTask());
    }

    [TestMethod(DisplayName = "重复 OperationId 应返回相同提交结果")]
    [Timeout(60_000)]
    public async Task RepeatedOperationIdShouldReturnSameCommittedRevision()
    {
        var context = await CreateContextAsync();
        var request = CreateRequest(
            context.Initial,
            "idempotent-add",
            [Add(CoursewareDraftEntityKind.SpacingToken, new CoursewareDraftEntityPayload
            {
                SpacingToken = new CoursewareSpacingToken { TokenId = "space-lg", Value = 48, Purpose = "大间距" },
            })]);

        var first = await context.Service.MutateAsync(request);
        var replay = await context.Service.MutateAsync(request);

        Assert.AreEqual(first.Revision.RevisionFingerprint, replay.Revision.RevisionFingerprint);
        Assert.IsFalse(replay.WasCommitted);
    }

    [TestMethod(DisplayName = "Work Item 范围外修改应被拒绝")]
    [Timeout(60_000)]
    public async Task WorkItemScopeShouldRejectOutOfScopeMutation()
    {
        var context = await CreateContextAsync();
        var request = CreateRequest(
            context.Initial,
            "out-of-scope",
            [Add(CoursewareDraftEntityKind.Component, ComponentPayload("new-component", "title"))],
            allowedEntityKinds: new HashSet<CoursewareDraftEntityKind> { CoursewareDraftEntityKind.SpacingToken });

        await Assert.ThrowsExactlyAsync<CoursewareDraftScopeViolationException>(
            () => context.Service.MutateAsync(request).AsTask());
    }

    [TestMethod(DisplayName = "无变化替换不应产生新 Revision")]
    [Timeout(60_000)]
    public async Task NoOpReplacementShouldNotCreateRevision()
    {
        var context = await CreateContextAsync();
        var component = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Component);
        var payload = (await context.Service.ReadEntityAsync(context.Initial.DraftId, component.EntityKey))!.Entities.Single();

        var result = await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "replace-no-op",
            [Replace(CoursewareDraftEntityKind.Component, component.EntityKey, payload)]));

        Assert.IsTrue(result.WasNoOp);
        Assert.HasCount(1, await context.Store.GetRevisionsAsync(context.Initial.DraftId));
    }

    [TestMethod(DisplayName = "no-op OperationId 不得用于不同 Revision")]
    [Timeout(60_000)]
    public async Task NoOpOperationIdShouldNotBeReusableForDifferentRevision()
    {
        var context = await CreateContextAsync();
        var component = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Component);
        var payload = (await context.Service.ReadEntityAsync(context.Initial.DraftId, component.EntityKey))!.Entities.Single();
        await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "no-op-id",
            [Replace(CoursewareDraftEntityKind.Component, component.EntityKey, payload)]));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => context.Service.MutateAsync(CreateRequest(
                context.Initial,
                "no-op-id",
                [Rename(CoursewareDraftEntityKind.Component, component.EntityKey, "different-component")])).AsTask());
    }

    [TestMethod(DisplayName = "初始 Revision 应拒绝重复 StableId")]
    [Timeout(60_000)]
    public void InitialRevisionShouldRejectDuplicateStableIds()
    {
        var designSystem = CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false);
        designSystem.Components = [designSystem.Components.Single(), designSystem.Components.Single() with { Name = "重复" }];

        Assert.ThrowsExactly<ArgumentException>(() => CoursewareDesignDraftRevision.CreateInitial("draft-duplicate", designSystem, CreatedAt));
    }

    [TestMethod(DisplayName = "完整 Revision 序列重放应保留相同指纹与 EntityKey")]
    [Timeout(60_000)]
    public async Task RevisionReplayShouldPreserveFingerprintAndEntityKeys()
    {
        var context = await CreateContextAsync();
        var component = context.Manifest.Entities.Single(item => item.Kind == CoursewareDraftEntityKind.Component);
        await context.Service.MutateAsync(CreateRequest(
            context.Initial,
            "rename-for-replay",
            [Rename(CoursewareDraftEntityKind.Component, component.EntityKey, "title-block-v2")]));
        var revisions = await context.Store.GetRevisionsAsync(context.Initial.DraftId);

        var replayed = revisions.Select(revision => new CoursewareDraftAggregate(revision)).Last();
        var persisted = new CoursewareDraftAggregate((await context.Store.GetLatestAsync(context.Initial.DraftId))!);

        Assert.AreEqual(persisted.Revision.RevisionFingerprint, replayed.Revision.RevisionFingerprint);
        CollectionAssert.AreEqual(
            persisted.GetManifest().Entities.Select(item => item.EntityKey.Value).ToArray(),
            replayed.GetManifest().Entities.Select(item => item.EntityKey.Value).ToArray());
    }

    [TestMethod(DisplayName = "Candidate Proposal 只应绑定指定 Revision")]
    [Timeout(60_000)]
    public async Task CandidateProposalShouldOnlyBindRequestedRevision()
    {
        var context = await CreateContextAsync();
        var proposal = await context.Service.ProposeCandidateAsync(
            context.Initial.DraftId,
            context.Initial.Revision,
            "candidate-1",
            "input-1",
            "quality-v1",
            CreatedAt.AddMinutes(1));

        Assert.AreEqual(context.Initial.RevisionFingerprint, proposal.Candidate.BaseRevision.RevisionFingerprint);
        Assert.AreEqual(context.Initial.RevisionFingerprint, proposal.Manifest.RevisionFingerprint);
    }

    private static async Task<TestContext> CreateContextAsync()
    {
        var store = new InMemoryCoursewareDesignDraftStore();
        var initial = CoursewareDesignDraftRevision.CreateInitial(
            "draft-1",
            CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false),
            CreatedAt);
        await store.CreateAsync(initial);
        var service = new CoursewareDraftMutationService(store);
        return new TestContext(store, service, initial, new CoursewareDraftAggregate(initial).GetManifest());
    }

    private static CoursewareDraftMutationRequest CreateRequest(
        CoursewareDesignDraftRevision revision,
        string operationId,
        IReadOnlyList<CoursewareDraftOperation> operations,
        IReadOnlySet<CoursewareDraftEntityKind>? allowedEntityKinds = null)
    {
        return new CoursewareDraftMutationRequest
        {
            OperationId = operationId,
            DraftId = revision.DraftId,
            ExpectedRevision = revision.Revision,
            CreatedAt = CreatedAt.AddMinutes(revision.Revision + 1),
            Scope = new CoursewareDraftOperationScope
            {
                WorkItemId = "work-item-1",
                Permissions = new CoursewareDraftWorkItemScope
                {
                    AllowedEntityKinds = allowedEntityKinds ?? Enum.GetValues<CoursewareDraftEntityKind>().ToHashSet(),
                    AllowedOperationKinds = Enum.GetValues<CoursewareDraftOperationKind>().ToHashSet(),
                },
            },
            Operations = operations,
        };
    }

    private static CoursewareDraftOperation Add(CoursewareDraftEntityKind kind, CoursewareDraftEntityPayload payload)
    {
        return new CoursewareDraftOperation { OperationKind = CoursewareDraftOperationKind.Add, EntityKind = kind, Payload = payload };
    }

    private static CoursewareDraftOperation Replace(
        CoursewareDraftEntityKind kind,
        CoursewareDraftEntityKey entityKey,
        CoursewareDraftEntityPayload payload)
    {
        return new CoursewareDraftOperation { OperationKind = CoursewareDraftOperationKind.Replace, EntityKind = kind, EntityKey = entityKey, Payload = payload };
    }

    private static CoursewareDraftOperation Delete(CoursewareDraftEntityKind kind, CoursewareDraftEntityKey entityKey)
    {
        return new CoursewareDraftOperation { OperationKind = CoursewareDraftOperationKind.Delete, EntityKind = kind, EntityKey = entityKey };
    }

    private static CoursewareDraftOperation Rename(
        CoursewareDraftEntityKind kind,
        CoursewareDraftEntityKey entityKey,
        string newStableId)
    {
        return new CoursewareDraftOperation
        {
            OperationKind = CoursewareDraftOperationKind.RenameStableId,
            EntityKind = kind,
            EntityKey = entityKey,
            NewStableId = newStableId,
        };
    }

    private static CoursewareDraftEntityPayload ComponentPayload(string componentId, string tokenId)
    {
        return new CoursewareDraftEntityPayload
        {
            Component = new CoursewareComponentSpecification
            {
                ComponentId = componentId,
                Name = componentId,
                Purpose = componentId,
                TokenIds = [tokenId],
            },
        };
    }

    private sealed record TestContext(
        InMemoryCoursewareDesignDraftStore Store,
        CoursewareDraftMutationService Service,
        CoursewareDesignDraftRevision Initial,
        CoursewareDraftManifest Manifest);
}
