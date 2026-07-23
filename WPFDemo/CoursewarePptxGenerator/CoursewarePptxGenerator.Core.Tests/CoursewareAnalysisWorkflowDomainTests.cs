using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGenerator.Core.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CoursewareAnalysisWorkflowDomainTests
{
    [TestMethod(DisplayName = "分析运行应按主链迁移并从可恢复旁路回到检查点")]
    [Timeout(60_000)]
    public void AnalysisRunShouldFollowMainFlowAndResumeRecoverableInterruptions()
    {
        var createdAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var run = CoursewareAnalysisRun.Create("run-1", "input-1", createdAt);
        var preparing = run.TransitionTo(CoursewareAnalysisRunState.PreparingInput, "PrepareInput", createdAt.AddMinutes(1));
        var canceled = preparing.TransitionTo(CoursewareAnalysisRunState.Canceled, "UserCanceled", createdAt.AddMinutes(2));

        Assert.AreEqual(CoursewareAnalysisRunState.PreparingInput, canceled.ResumeState);
        Assert.IsFalse(canceled.IsTerminal);

        var resumed = canceled.Resume("ContinueFromCheckpoint", createdAt.AddMinutes(3));
        var authoring = resumed.TransitionTo(CoursewareAnalysisRunState.Authoring, "InputPrepared", createdAt.AddMinutes(4));
        var candidateReady = authoring.TransitionTo(CoursewareAnalysisRunState.CandidateReady, "CandidateProposed", createdAt.AddMinutes(5));
        var evaluating = candidateReady.TransitionTo(CoursewareAnalysisRunState.Evaluating, "EvaluateCandidate", createdAt.AddMinutes(6));
        var exhausted = evaluating.TransitionTo(CoursewareAnalysisRunState.Exhausted, "BudgetExhausted", createdAt.AddMinutes(7));

        Assert.AreEqual(CoursewareAnalysisRunState.Evaluating, exhausted.ResumeState);
        Assert.AreEqual(7L, exhausted.Version);
        Assert.AreEqual(CoursewareAnalysisRunState.Evaluating, exhausted.Resume("BudgetIncreased", createdAt.AddMinutes(8)).State);
    }

    [TestMethod(DisplayName = "已发布运行不得再次迁移或恢复")]
    [Timeout(60_000)]
    public void PublishedRunShouldBeTerminal()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var run = CoursewareAnalysisRun.Create("run-1", "input-1", now)
            .TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(1))
            .TransitionTo(CoursewareAnalysisRunState.Authoring, "Author", now.AddMinutes(2))
            .TransitionTo(CoursewareAnalysisRunState.CandidateReady, "Candidate", now.AddMinutes(3))
            .TransitionTo(CoursewareAnalysisRunState.Evaluating, "Evaluate", now.AddMinutes(4))
            .TransitionTo(CoursewareAnalysisRunState.Qualified, "Qualified", now.AddMinutes(5))
            .TransitionTo(CoursewareAnalysisRunState.Publishing, "Publish", now.AddMinutes(6))
            .TransitionTo(CoursewareAnalysisRunState.Published, "Published", now.AddMinutes(7));

        Assert.IsTrue(run.IsTerminal);
        Assert.IsFalse(run.CanTransitionTo(CoursewareAnalysisRunState.Superseded));
        Assert.ThrowsExactly<InvalidOperationException>(() => run.Resume("Invalid", now.AddMinutes(8)));
    }

    [TestMethod(DisplayName = "分析运行不得跳过候选评估或从旁路切换到任意状态")]
    [Timeout(60_000)]
    public void AnalysisRunShouldRejectInvalidTransitions()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var created = CoursewareAnalysisRun.Create("run-1", "input-1", now);

        Assert.ThrowsExactly<InvalidOperationException>(() => created.TransitionTo(
            CoursewareAnalysisRunState.Authoring,
            "SkipInputPreparation",
            now.AddMinutes(1)));

        var blocked = created
            .TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(1))
            .TransitionTo(CoursewareAnalysisRunState.InfrastructureBlocked, "EndpointUnavailable", now.AddMinutes(2));

        Assert.IsFalse(blocked.CanTransitionTo(CoursewareAnalysisRunState.Authoring));
        Assert.ThrowsExactly<InvalidOperationException>(() => blocked.TransitionTo(
            CoursewareAnalysisRunState.Authoring,
            "InvalidResume",
            now.AddMinutes(3)));
    }

    [TestMethod(DisplayName = "缺失或 NotSupported 的必需质量门不得创建 Qualified Artifact")]
    [Timeout(60_000)]
    public void MissingOrUnsupportedRequiredGateShouldBlockQualification()
    {
        var candidate = CreateCandidate(out var now);
        var unsupportedReport = CreateGateReport(
            candidate,
            "G6",
            CoursewareQualityGateOutcome.NotSupported,
            now.AddMinutes(1));

        Assert.ThrowsExactly<InvalidOperationException>(() => QualifiedCoursewareArtifact.Create(
            "qualified-1",
            candidate,
            ["G2", "G6"],
            [unsupportedReport],
            now.AddMinutes(2)));
    }

    [TestMethod(DisplayName = "质量门报告必须绑定同一 Candidate 指纹且 GateId 唯一")]
    [Timeout(60_000)]
    public void QualificationShouldRejectMismatchedCandidateAndDuplicateGateReports()
    {
        var candidate = CreateCandidate(out var now);
        var mismatchedReport = CreateGateReport(
            candidate,
            "G2",
            CoursewareQualityGateOutcome.Passed,
            now.AddMinutes(1)) with
        {
            CandidateFingerprint = "different-candidate",
        };
        Assert.ThrowsExactly<InvalidOperationException>(() => QualifiedCoursewareArtifact.Create(
            "qualified-1",
            candidate,
            ["G2"],
            [mismatchedReport],
            now.AddMinutes(2)));

        var report = CreateGateReport(candidate, "G2", CoursewareQualityGateOutcome.Passed, now.AddMinutes(1));
        Assert.ThrowsExactly<InvalidOperationException>(() => QualifiedCoursewareArtifact.Create(
            "qualified-2",
            candidate,
            ["G2"],
            [report, report],
            now.AddMinutes(2)));
    }

    [TestMethod(DisplayName = "发布失败后只能重试同一发布阶段")]
    [Timeout(60_000)]
    public void PublicationFailureShouldOnlyRetryPublishing()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var failed = CoursewareAnalysisRun.Create("run-1", "input-1", now)
            .TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(1))
            .TransitionTo(CoursewareAnalysisRunState.Authoring, "Author", now.AddMinutes(2))
            .TransitionTo(CoursewareAnalysisRunState.CandidateReady, "Candidate", now.AddMinutes(3))
            .TransitionTo(CoursewareAnalysisRunState.Evaluating, "Evaluate", now.AddMinutes(4))
            .TransitionTo(CoursewareAnalysisRunState.Qualified, "Qualified", now.AddMinutes(5))
            .TransitionTo(CoursewareAnalysisRunState.Publishing, "Publish", now.AddMinutes(6))
            .TransitionTo(CoursewareAnalysisRunState.PublicationFailed, "StorageUnavailable", now.AddMinutes(7));

        Assert.IsTrue(failed.CanTransitionTo(CoursewareAnalysisRunState.Publishing));
        Assert.IsFalse(failed.CanTransitionTo(CoursewareAnalysisRunState.Evaluating));
        Assert.AreEqual(
            CoursewareAnalysisRunState.Publishing,
            failed.TransitionTo(CoursewareAnalysisRunState.Publishing, "RetrySameArtifact", now.AddMinutes(8)).State);
    }

    [TestMethod(DisplayName = "状态迁移与恢复时间不得早于当前检查点")]
    [Timeout(60_000)]
    public void AnalysisRunShouldRequireMonotonicTransitionTime()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var preparing = CoursewareAnalysisRun.Create("run-1", "input-1", now)
            .TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(2));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => preparing.TransitionTo(
            CoursewareAnalysisRunState.Authoring,
            "TimeMovedBackwards",
            now.AddMinutes(1)));

        var canceled = preparing.TransitionTo(CoursewareAnalysisRunState.Canceled, "Cancel", now.AddMinutes(3));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => canceled.Resume("ResumeTooEarly", now.AddMinutes(2)));
    }

    [TestMethod(DisplayName = "Run Store 应按预期版本提交并拒绝陈旧写入")]
    [Timeout(60_000)]
    public async Task AnalysisRunStoreShouldEnforceOptimisticConcurrency()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new InMemoryCoursewareAnalysisRunStore();
        var created = CoursewareAnalysisRun.Create("run-1", "input-1", now);
        await store.AddAsync(created);

        var preparing = created.TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(1));
        await store.UpdateAsync(preparing, created.Version);

        var stored = await store.GetAsync(created.RunId);
        Assert.AreEqual(preparing, stored);

        var staleUpdate = created.TransitionTo(CoursewareAnalysisRunState.PreparingInput, "StalePrepare", now.AddMinutes(2));
        var exception = await Assert.ThrowsExactlyAsync<CoursewareAnalysisRunConcurrencyException>(
            () => store.UpdateAsync(staleUpdate, created.Version).AsTask());
        Assert.AreEqual(preparing.Version, exception.ActualVersion);
    }

    [TestMethod(DisplayName = "Run Store 应拒绝重复创建和非连续版本")]
    [Timeout(60_000)]
    public async Task AnalysisRunStoreShouldRejectDuplicateAndNonSequentialSnapshots()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new InMemoryCoursewareAnalysisRunStore();
        var created = CoursewareAnalysisRun.Create("run-1", "input-1", now);
        await store.AddAsync(created);

        await Assert.ThrowsExactlyAsync<CoursewareAnalysisRunConcurrencyException>(
            () => store.AddAsync(created).AsTask());

        var authoring = created
            .TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(1))
            .TransitionTo(CoursewareAnalysisRunState.Authoring, "Author", now.AddMinutes(2));
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => store.UpdateAsync(authoring, created.Version).AsTask());
    }

    [TestMethod(DisplayName = "Run Store 取消操作不得修改已提交快照")]
    [Timeout(60_000)]
    public async Task AnalysisRunStoreCancellationShouldNotMutateState()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new InMemoryCoursewareAnalysisRunStore();
        var created = CoursewareAnalysisRun.Create("run-1", "input-1", now);
        await store.AddAsync(created);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var preparing = created.TransitionTo(CoursewareAnalysisRunState.PreparingInput, "Prepare", now.AddMinutes(1));
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => store.UpdateAsync(preparing, created.Version, cancellationTokenSource.Token).AsTask());

        Assert.AreEqual(created, await store.GetAsync(created.RunId));
    }

    [TestMethod(DisplayName = "草稿修订应隔离外部修改并拒绝无变化 Revision")]
    [Timeout(60_000)]
    public void DraftRevisionShouldIsolatePayloadAndRejectNoOpRevision()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var source = CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false);
        var revision = CoursewareDesignDraftRevision.CreateInitial("draft-1", source, now);

        source.DesignSystemId = "mutated-outside";
        Assert.AreEqual("design-system", revision.DesignSystem.DesignSystemId);

        var snapshot = revision.DesignSystem;
        snapshot.DesignSystemId = "mutated-snapshot";
        Assert.AreEqual("design-system", revision.DesignSystem.DesignSystemId);
        Assert.ThrowsExactly<InvalidOperationException>(() => revision.CreateNext(revision.DesignSystem, now.AddMinutes(1)));

        var changed = revision.DesignSystem;
        changed.DesignSystemId = "design-system-v2";
        var next = revision.CreateNext(changed, now.AddMinutes(1));

        Assert.AreEqual(1L, next.Revision);
        Assert.AreEqual(revision.RevisionFingerprint, next.ParentRevisionFingerprint);
        Assert.AreNotEqual(revision.RevisionFingerprint, next.RevisionFingerprint);
    }

    [TestMethod(DisplayName = "Draft Store 应幂等提交同一 OperationId 并保留 Revision 顺序")]
    [Timeout(60_000)]
    public async Task DraftStoreShouldCommitOperationsIdempotentlyAndPreserveRevisionOrder()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new InMemoryCoursewareDesignDraftStore();
        var initial = CoursewareDesignDraftRevision.CreateInitial(
            "draft-1",
            CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false),
            now);
        await store.CreateAsync(initial);
        var changedDesignSystem = initial.DesignSystem;
        changedDesignSystem.DesignSystemId = "design-system-v2";
        var next = initial.CreateNext(changedDesignSystem, now.AddMinutes(1));

        var committed = await store.CommitAsync("operation-1", initial.Revision, next);
        var replayed = await store.CommitAsync("operation-1", initial.Revision, next);
        var revisions = await store.GetRevisionsAsync(initial.DraftId);

        Assert.IsTrue(committed.WasCommitted);
        Assert.IsFalse(replayed.WasCommitted);
        CollectionAssert.AreEqual(new long[] { 0, 1 }, revisions.Select(item => item.Revision).ToArray());
    }

    [TestMethod(DisplayName = "Draft Store 应拒绝陈旧 Revision 和断链提交")]
    [Timeout(60_000)]
    public async Task DraftStoreShouldRejectStaleAndDetachedRevisions()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new InMemoryCoursewareDesignDraftStore();
        var initial = CoursewareDesignDraftRevision.CreateInitial(
            "draft-1",
            CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false),
            now);
        await store.CreateAsync(initial);
        var firstDesignSystem = initial.DesignSystem;
        firstDesignSystem.DesignSystemId = "design-system-v2";
        var first = initial.CreateNext(firstDesignSystem, now.AddMinutes(1));
        await store.CommitAsync("operation-1", initial.Revision, first);

        var staleDesignSystem = initial.DesignSystem;
        staleDesignSystem.DesignSystemId = "stale-design-system";
        var stale = initial.CreateNext(staleDesignSystem, now.AddMinutes(2));
        await Assert.ThrowsExactlyAsync<CoursewareDesignDraftConcurrencyException>(
            () => store.CommitAsync("operation-2", initial.Revision, stale).AsTask());

        var alternateFirstDesignSystem = initial.DesignSystem;
        alternateFirstDesignSystem.DesignSystemId = "alternate-design-system-v2";
        var alternateFirst = initial.CreateNext(alternateFirstDesignSystem, now.AddMinutes(1));
        var detachedDesignSystem = alternateFirst.DesignSystem;
        detachedDesignSystem.DesignSystemId = "detached-design-system-v3";
        var detached = alternateFirst.CreateNext(detachedDesignSystem, now.AddMinutes(2));
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => store.CommitAsync("operation-3", first.Revision, detached).AsTask());
    }

    [TestMethod(DisplayName = "Draft Store 不得将同一 OperationId 用于不同 Revision")]
    [Timeout(60_000)]
    public async Task DraftStoreShouldRejectOperationIdReuseForDifferentRevision()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new InMemoryCoursewareDesignDraftStore();
        var initial = CoursewareDesignDraftRevision.CreateInitial(
            "draft-1",
            CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false),
            now);
        await store.CreateAsync(initial);
        var firstDesignSystem = initial.DesignSystem;
        firstDesignSystem.DesignSystemId = "design-system-v2";
        var first = initial.CreateNext(firstDesignSystem, now.AddMinutes(1));
        await store.CommitAsync("operation-1", initial.Revision, first);

        var conflictingDesignSystem = initial.DesignSystem;
        conflictingDesignSystem.DesignSystemId = "different-design-system";
        var conflicting = initial.CreateNext(conflictingDesignSystem, now.AddMinutes(1));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => store.CommitAsync("operation-1", initial.Revision, conflicting).AsTask());
    }

    [TestMethod(DisplayName = "Candidate 指纹应绑定输入策略与 Revision")]
    [Timeout(60_000)]
    public void CandidateFingerprintShouldBindInputPolicyAndRevision()
    {
        var now = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var revision = CoursewareDesignDraftRevision.CreateInitial(
            "draft-1",
            CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false),
            now);

        var first = CoursewareCandidate.Create("candidate-1", "input-1", "production-v1", revision, now);
        var samePayload = CoursewareCandidate.Create("candidate-2", "input-1", "production-v1", revision, now);
        var differentPolicy = CoursewareCandidate.Create("candidate-3", "input-1", "production-v2", revision, now);

        Assert.AreEqual(first.CandidateFingerprint, samePayload.CandidateFingerprint);
        Assert.AreNotEqual(first.CandidateFingerprint, differentPolicy.CandidateFingerprint);
    }

    [TestMethod(DisplayName = "任一必需质量门失败都不得创建 Qualified Artifact")]
    [Timeout(60_000)]
    public void FailedRequiredGateShouldBlockQualification()
    {
        var candidate = CreateCandidate(out var now);
        var reports = new[]
        {
            CreateGateReport(candidate, "G2", CoursewareQualityGateOutcome.Passed, now.AddMinutes(1)),
            CreateGateReport(candidate, "G3", CoursewareQualityGateOutcome.Failed, now.AddMinutes(2)),
        };

        Assert.ThrowsExactly<InvalidOperationException>(() => QualifiedCoursewareArtifact.Create(
            "qualified-1",
            candidate,
            ["G2", "G3"],
            reports,
            now.AddMinutes(3)));
    }

    [TestMethod(DisplayName = "已验证 Manifest 必须匹配 Qualified Artifact 且指纹不可篡改")]
    [Timeout(60_000)]
    public void PublishedArtifactShouldRequireMatchingVerifiedManifest()
    {
        var candidate = CreateCandidate(out var now);
        var report = CreateGateReport(candidate, "G2", CoursewareQualityGateOutcome.Passed, now.AddMinutes(1));
        var qualified = QualifiedCoursewareArtifact.Create(
            "qualified-1",
            candidate,
            ["G2"],
            [report],
            now.AddMinutes(2));
        var manifest = new CoursewareArtifactManifest
        {
            ArtifactId = "artifact-1",
            QualifiedArtifactId = qualified.QualifiedArtifactId,
            CandidateFingerprint = candidate.CandidateFingerprint,
            InputFingerprint = candidate.InputFingerprint,
            QualityPolicyVersion = candidate.QualityPolicyVersion,
            IsReadBackVerified = true,
        };
        manifest = manifest with { ManifestFingerprint = CoursewareArtifactManifestFingerprint.Compute(manifest) };

        var published = PublishedCoursewareArtifact.Create(qualified, manifest, now.AddMinutes(3));
        Assert.AreEqual("artifact-1", published.Manifest.ArtifactId);

        var tampered = manifest with { ArtifactId = "artifact-2" };
        Assert.ThrowsExactly<InvalidOperationException>(() => PublishedCoursewareArtifact.Create(
            qualified,
            tampered,
            now.AddMinutes(3)));
    }

    [TestMethod(DisplayName = "未回读验证的 Manifest 不得创建 Published Artifact")]
    [Timeout(60_000)]
    public void PublishedArtifactShouldRejectUnverifiedManifest()
    {
        var candidate = CreateCandidate(out var now);
        var report = CreateGateReport(candidate, "G2", CoursewareQualityGateOutcome.Passed, now.AddMinutes(1));
        var qualified = QualifiedCoursewareArtifact.Create(
            "qualified-1",
            candidate,
            ["G2"],
            [report],
            now.AddMinutes(2));
        var manifest = new CoursewareArtifactManifest
        {
            ArtifactId = "artifact-1",
            QualifiedArtifactId = qualified.QualifiedArtifactId,
            CandidateFingerprint = candidate.CandidateFingerprint,
            InputFingerprint = candidate.InputFingerprint,
            QualityPolicyVersion = candidate.QualityPolicyVersion,
            IsReadBackVerified = false,
        };
        manifest = manifest with { ManifestFingerprint = CoursewareArtifactManifestFingerprint.Compute(manifest) };

        Assert.ThrowsExactly<InvalidOperationException>(() => PublishedCoursewareArtifact.Create(
            qualified,
            manifest,
            now.AddMinutes(3)));
    }

    private static CoursewareCandidate CreateCandidate(out DateTimeOffset createdAt)
    {
        createdAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var revision = CoursewareDesignDraftRevision.CreateInitial(
            "draft-1",
            CoursewareExecutableDesignSystemTests.CreateValidDesignSystem(includeVisualEvidence: false),
            createdAt);
        return CoursewareCandidate.Create("candidate-1", "input-1", "production-v1", revision, createdAt);
    }

    private static CoursewareQualityGateReport CreateGateReport(
        CoursewareCandidate candidate,
        string gateId,
        CoursewareQualityGateOutcome outcome,
        DateTimeOffset completedAt)
    {
        return new CoursewareQualityGateReport
        {
            GateId = gateId,
            CandidateFingerprint = candidate.CandidateFingerprint,
            QualityPolicyVersion = candidate.QualityPolicyVersion,
            EvaluatorVersion = "test-v1",
            IsRequired = true,
            Outcome = outcome,
            CompletedAt = completedAt,
        };
    }
}
