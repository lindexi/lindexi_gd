using System.Security.Cryptography;
using System.Text;

namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Defines the result of one quality gate evaluation.
/// </summary>
public enum CoursewareQualityGateOutcome
{
    /// <summary>The quality gate has not executed.</summary>
    NotRun,

    /// <summary>The candidate passed the quality gate.</summary>
    Passed,

    /// <summary>The candidate failed the quality gate.</summary>
    Failed,

    /// <summary>The current environment cannot execute the quality gate.</summary>
    NotSupported,
}

/// <summary>
/// Identifies one immutable design-system candidate evaluated by quality gates.
/// </summary>
public sealed record CoursewareCandidate
{
    /// <summary>The candidate fingerprint algorithm version.</summary>
    public const string FingerprintAlgorithmVersion = "sha256-candidate-v1";

    private CoursewareCandidate(
        string candidateId,
        string inputFingerprint,
        string qualityPolicyVersion,
        CoursewareDesignDraftRevision baseRevision,
        string candidateFingerprint,
        DateTimeOffset createdAt)
    {
        CandidateId = candidateId;
        InputFingerprint = inputFingerprint;
        QualityPolicyVersion = qualityPolicyVersion;
        BaseRevision = baseRevision;
        CandidateFingerprint = candidateFingerprint;
        CreatedAt = createdAt;
    }

    /// <summary>Gets the stable candidate identifier.</summary>
    public string CandidateId { get; }

    /// <summary>Gets the immutable input fingerprint.</summary>
    public string InputFingerprint { get; }

    /// <summary>Gets the quality policy version that governs qualification.</summary>
    public string QualityPolicyVersion { get; }

    /// <summary>Gets the immutable draft revision submitted for evaluation.</summary>
    public CoursewareDesignDraftRevision BaseRevision { get; }

    /// <summary>Gets the stable candidate fingerprint.</summary>
    public string CandidateFingerprint { get; }

    /// <summary>Gets when the candidate was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Creates a candidate from one immutable draft revision.
    /// </summary>
    public static CoursewareCandidate Create(
        string candidateId,
        string inputFingerprint,
        string qualityPolicyVersion,
        CoursewareDesignDraftRevision baseRevision,
        DateTimeOffset createdAt)
    {
        ThrowIfNullOrWhiteSpace(candidateId, nameof(candidateId));
        ThrowIfNullOrWhiteSpace(inputFingerprint, nameof(inputFingerprint));
        ThrowIfNullOrWhiteSpace(qualityPolicyVersion, nameof(qualityPolicyVersion));
        ArgumentNullException.ThrowIfNull(baseRevision);
        if (createdAt < baseRevision.CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAt), createdAt, "Candidate creation time cannot precede its base revision.");
        }

        return new CoursewareCandidate(
            candidateId,
            inputFingerprint,
            qualityPolicyVersion,
            baseRevision,
            ComputeFingerprint(inputFingerprint, qualityPolicyVersion, baseRevision.RevisionFingerprint),
            createdAt);
    }

    private static string ComputeFingerprint(
        string inputFingerprint,
        string qualityPolicyVersion,
        string revisionFingerprint)
    {
        var canonicalValue = string.Join(
            '\0',
            FingerprintAlgorithmVersion,
            inputFingerprint,
            qualityPolicyVersion,
            revisionFingerprint);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValue))).ToLowerInvariant();
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}

/// <summary>
/// Represents the verified manifest produced by an atomic artifact publication.
/// </summary>
public sealed record CoursewareArtifactManifest
{
    /// <summary>Gets the published artifact identifier.</summary>
    public string ArtifactId { get; init; } = string.Empty;

    /// <summary>Gets the qualified artifact identifier used as the publication source.</summary>
    public string QualifiedArtifactId { get; init; } = string.Empty;

    /// <summary>Gets the published candidate fingerprint.</summary>
    public string CandidateFingerprint { get; init; } = string.Empty;

    /// <summary>Gets the published input fingerprint.</summary>
    public string InputFingerprint { get; init; } = string.Empty;

    /// <summary>Gets the quality policy version proven by the artifact.</summary>
    public string QualityPolicyVersion { get; init; } = string.Empty;

    /// <summary>Gets the deterministic manifest fingerprint calculated by the publisher.</summary>
    public string ManifestFingerprint { get; init; } = string.Empty;

    /// <summary>Gets whether the artifact was successfully read back and verified.</summary>
    public bool IsReadBackVerified { get; init; }
}

/// <summary>
/// Represents an atomically published and read-back-verified courseware artifact.
/// </summary>
public sealed record PublishedCoursewareArtifact
{
    private PublishedCoursewareArtifact(
        QualifiedCoursewareArtifact qualifiedArtifact,
        CoursewareArtifactManifest manifest,
        DateTimeOffset publishedAt)
    {
        QualifiedArtifact = qualifiedArtifact;
        Manifest = manifest;
        PublishedAt = publishedAt;
    }

    /// <summary>Gets the qualified source that was published.</summary>
    public QualifiedCoursewareArtifact QualifiedArtifact { get; }

    /// <summary>Gets the verified publication manifest.</summary>
    public CoursewareArtifactManifest Manifest { get; }

    /// <summary>Gets when atomic publication completed.</summary>
    public DateTimeOffset PublishedAt { get; }

    /// <summary>
    /// Creates a published artifact only from a qualified artifact and a matching verified manifest.
    /// </summary>
    public static PublishedCoursewareArtifact Create(
        QualifiedCoursewareArtifact qualifiedArtifact,
        CoursewareArtifactManifest manifest,
        DateTimeOffset publishedAt)
    {
        ArgumentNullException.ThrowIfNull(qualifiedArtifact);
        ArgumentNullException.ThrowIfNull(manifest);

        ThrowIfNullOrWhiteSpace(manifest.ArtifactId, nameof(manifest.ArtifactId));
        ThrowIfNullOrWhiteSpace(manifest.QualifiedArtifactId, nameof(manifest.QualifiedArtifactId));
        ThrowIfNullOrWhiteSpace(manifest.CandidateFingerprint, nameof(manifest.CandidateFingerprint));
        ThrowIfNullOrWhiteSpace(manifest.InputFingerprint, nameof(manifest.InputFingerprint));
        ThrowIfNullOrWhiteSpace(manifest.QualityPolicyVersion, nameof(manifest.QualityPolicyVersion));
        ThrowIfNullOrWhiteSpace(manifest.ManifestFingerprint, nameof(manifest.ManifestFingerprint));

        if (!manifest.IsReadBackVerified)
        {
            throw new InvalidOperationException("The artifact manifest was not read-back verified.");
        }

        if (!string.Equals(
                manifest.QualifiedArtifactId,
                qualifiedArtifact.QualifiedArtifactId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The artifact manifest refers to a different qualified artifact.");
        }

        var expectedManifestFingerprint = CoursewareArtifactManifestFingerprint.Compute(manifest);
        if (!string.Equals(manifest.ManifestFingerprint, expectedManifestFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The artifact manifest fingerprint is invalid.");
        }

        var candidate = qualifiedArtifact.Candidate;
        if (!string.Equals(manifest.CandidateFingerprint, candidate.CandidateFingerprint, StringComparison.Ordinal)
            || !string.Equals(manifest.InputFingerprint, candidate.InputFingerprint, StringComparison.Ordinal)
            || !string.Equals(manifest.QualityPolicyVersion, candidate.QualityPolicyVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The artifact manifest fingerprints do not match the qualified candidate.");
        }

        if (publishedAt < qualifiedArtifact.QualifiedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(publishedAt), publishedAt, "Publication time cannot precede qualification.");
        }

        return new PublishedCoursewareArtifact(qualifiedArtifact, manifest, publishedAt);
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}

/// <summary>
/// Represents the immutable result of one quality gate for one candidate.
/// </summary>
public sealed record CoursewareQualityGateReport
{
    /// <summary>Gets the stable quality gate identifier.</summary>
    public string GateId { get; init; } = string.Empty;

    /// <summary>Gets the evaluated candidate fingerprint.</summary>
    public string CandidateFingerprint { get; init; } = string.Empty;

    /// <summary>Gets the quality policy version used by the gate.</summary>
    public string QualityPolicyVersion { get; init; } = string.Empty;

    /// <summary>Gets the validator, compiler, renderer, or reviewer version.</summary>
    public string EvaluatorVersion { get; init; } = string.Empty;

    /// <summary>Gets whether this gate is required for product publication.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Gets the gate outcome.</summary>
    public CoursewareQualityGateOutcome Outcome { get; init; }

    /// <summary>Gets when evaluation completed.</summary>
    public DateTimeOffset CompletedAt { get; init; }

    /// <summary>Gets structured deterministic diagnostics emitted by the gate.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Gets whether this report qualifies the required gate for publication.</summary>
    public bool IsPassingRequiredGate => !IsRequired || Outcome == CoursewareQualityGateOutcome.Passed;
}

/// <summary>
/// Represents a candidate that passed every quality gate required by its quality policy.
/// </summary>
public sealed record QualifiedCoursewareArtifact
{
    private QualifiedCoursewareArtifact(
        string qualifiedArtifactId,
        CoursewareCandidate candidate,
        IReadOnlyList<CoursewareQualityGateReport> gateReports,
        DateTimeOffset qualifiedAt)
    {
        QualifiedArtifactId = qualifiedArtifactId;
        Candidate = candidate;
        GateReports = gateReports.ToArray();
        QualifiedAt = qualifiedAt;
    }

    /// <summary>Gets the stable qualified-artifact identifier.</summary>
    public string QualifiedArtifactId { get; }

    /// <summary>Gets the candidate proven to satisfy the quality policy.</summary>
    public CoursewareCandidate Candidate { get; }

    /// <summary>Gets the complete quality evidence used for qualification.</summary>
    public IReadOnlyList<CoursewareQualityGateReport> GateReports { get; }

    /// <summary>Gets when qualification completed.</summary>
    public DateTimeOffset QualifiedAt { get; }

    /// <summary>
    /// Qualifies a candidate only when every required gate is present and passed for the same fingerprint.
    /// </summary>
    public static QualifiedCoursewareArtifact Create(
        string qualifiedArtifactId,
        CoursewareCandidate candidate,
        IReadOnlyCollection<string> requiredGateIds,
        IReadOnlyCollection<CoursewareQualityGateReport> gateReports,
        DateTimeOffset qualifiedAt)
    {
        ThrowIfNullOrWhiteSpace(qualifiedArtifactId, nameof(qualifiedArtifactId));
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(requiredGateIds);
        ArgumentNullException.ThrowIfNull(gateReports);

        var requiredGateIdSet = ValidateRequiredGateIds(requiredGateIds);
        var reportsByGateId = ValidateGateReports(candidate, gateReports);
        var missingGateIds = requiredGateIdSet
            .Where(gateId => !reportsByGateId.ContainsKey(gateId))
            .OrderBy(gateId => gateId, StringComparer.Ordinal)
            .ToArray();
        if (missingGateIds.Length > 0)
        {
            throw new InvalidOperationException($"Required quality gate reports are missing: {string.Join(", ", missingGateIds)}.");
        }

        var invalidRequiredGateIds = requiredGateIdSet
            .Where(gateId =>
            {
                var report = reportsByGateId[gateId];
                return !report.IsRequired || report.Outcome != CoursewareQualityGateOutcome.Passed;
            })
            .OrderBy(gateId => gateId, StringComparer.Ordinal)
            .ToArray();
        if (invalidRequiredGateIds.Length > 0)
        {
            throw new InvalidOperationException($"Required quality gates did not pass: {string.Join(", ", invalidRequiredGateIds)}.");
        }

        if (qualifiedAt < candidate.CreatedAt || gateReports.Any(report => qualifiedAt < report.CompletedAt))
        {
            throw new ArgumentOutOfRangeException(nameof(qualifiedAt), qualifiedAt, "Qualification time cannot precede the candidate or its gate reports.");
        }

        return new QualifiedCoursewareArtifact(
            qualifiedArtifactId,
            candidate,
            gateReports.OrderBy(report => report.GateId, StringComparer.Ordinal).ToArray(),
            qualifiedAt);
    }

    private static IReadOnlySet<string> ValidateRequiredGateIds(IReadOnlyCollection<string> requiredGateIds)
    {
        if (requiredGateIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Required quality gate identifiers cannot be empty.", nameof(requiredGateIds));
        }

        var distinctGateIds = requiredGateIds.ToHashSet(StringComparer.Ordinal);
        if (distinctGateIds.Count != requiredGateIds.Count)
        {
            throw new ArgumentException("Required quality gate identifiers must be unique.", nameof(requiredGateIds));
        }

        return distinctGateIds;
    }

    private static IReadOnlyDictionary<string, CoursewareQualityGateReport> ValidateGateReports(
        CoursewareCandidate candidate,
        IReadOnlyCollection<CoursewareQualityGateReport> gateReports)
    {
        if (gateReports.Any(report => report is null))
        {
            throw new ArgumentException("Quality gate reports cannot contain null values.", nameof(gateReports));
        }

        var duplicateGateIds = gateReports
            .GroupBy(report => report.GateId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(gateId => gateId, StringComparer.Ordinal)
            .ToArray();
        if (duplicateGateIds.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate quality gate reports were supplied: {string.Join(", ", duplicateGateIds)}.");
        }

        foreach (var report in gateReports)
        {
            ThrowIfNullOrWhiteSpace(report.GateId, nameof(report.GateId));
            ThrowIfNullOrWhiteSpace(report.CandidateFingerprint, nameof(report.CandidateFingerprint));
            ThrowIfNullOrWhiteSpace(report.QualityPolicyVersion, nameof(report.QualityPolicyVersion));
            ThrowIfNullOrWhiteSpace(report.EvaluatorVersion, nameof(report.EvaluatorVersion));

            if (!string.Equals(report.CandidateFingerprint, candidate.CandidateFingerprint, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Quality gate '{report.GateId}' evaluated a different candidate fingerprint.");
            }

            if (!string.Equals(report.QualityPolicyVersion, candidate.QualityPolicyVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Quality gate '{report.GateId}' used a different quality policy version.");
            }

            if (report.CompletedAt < candidate.CreatedAt)
            {
                throw new InvalidOperationException($"Quality gate '{report.GateId}' completed before the candidate was created.");
            }
        }

        return gateReports.ToDictionary(report => report.GateId, StringComparer.Ordinal);
    }

    private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }
    }
}

/// <summary>
/// Computes the deterministic fingerprint of a publication manifest.
/// </summary>
public static class CoursewareArtifactManifestFingerprint
{
    /// <summary>The manifest fingerprint algorithm version.</summary>
    public const string AlgorithmVersion = "sha256-artifact-manifest-v1";

    /// <summary>Computes a fingerprint from all publication identity fields except the fingerprint itself.</summary>
    public static string Compute(CoursewareArtifactManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var canonicalValue = string.Join(
            '\0',
            AlgorithmVersion,
            manifest.ArtifactId,
            manifest.QualifiedArtifactId,
            manifest.CandidateFingerprint,
            manifest.InputFingerprint,
            manifest.QualityPolicyVersion,
            manifest.IsReadBackVerified ? "1" : "0");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValue))).ToLowerInvariant();
    }
}
