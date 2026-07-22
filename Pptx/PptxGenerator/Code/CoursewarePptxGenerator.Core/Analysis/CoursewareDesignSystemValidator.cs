using System.Text.Json;
using System.Text.RegularExpressions;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Performs deterministic cross-object validation for an executable courseware design system.
/// </summary>
public sealed class CoursewareDesignSystemValidator
{
    private static readonly Regex HexColorRegex = new(
        "^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates a design system against the complete input identity, resource catalog and visual evidence boundary.
    /// </summary>
    public CoursewareDesignSystemValidationReport Validate(
        CoursewareDesignSystem designSystem,
        IEnumerable<string> inputSlideIds,
        IEnumerable<string> knownResourceIds,
        bool visualAnalysisExecuted)
    {
        ArgumentNullException.ThrowIfNull(designSystem);
        ArgumentNullException.ThrowIfNull(inputSlideIds);
        ArgumentNullException.ThrowIfNull(knownResourceIds);

        var diagnostics = new List<CoursewareValidationDiagnostic>();
        var expectedSlideIds = inputSlideIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var resources = knownResourceIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        RequireEqual(designSystem.SchemaVersion, CoursewareDesignSystem.CurrentSchemaVersion, "SchemaVersionMismatch", "schemaVersion", diagnostics);
        RequireText(designSystem.DesignSystemId, "DesignSystemIdRequired", "designSystemId", diagnostics);
        RequireText(designSystem.DesignIntent.Name, "DesignIntentNameRequired", "designIntent.name", diagnostics);
        RequireText(designSystem.DesignIntent.Summary, "DesignIntentSummaryRequired", "designIntent.summary", diagnostics);
        RequireText(designSystem.DesignIntent.Rationale, "DesignIntentRationaleRequired", "designIntent.rationale", diagnostics);

        ValidateUniqueIds(designSystem.CanvasProfiles.Select(item => item.CanvasId), "canvasProfiles", diagnostics);
        if (designSystem.CanvasProfiles.Count == 0 || designSystem.CanvasProfiles.Count(item => item.IsPrimary) != 1)
        {
            AddError("PrimaryCanvasRequired", "canvasProfiles", "必须且只能定义一个主画布。", diagnostics);
        }

        foreach (var canvas in designSystem.CanvasProfiles)
        {
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                AddError("CanvasSizeInvalid", $"canvasProfiles[{canvas.CanvasId}]", "画布宽高必须大于零。", diagnostics);
            }
        }

        if (designSystem.Grid.ColumnCount <= 0 || designSystem.Grid.Gutter < 0 || designSystem.Grid.Baseline <= 0)
        {
            AddError("GridInvalid", "grid", "网格列数和基线必须大于零，栏间距不得为负数。", diagnostics);
        }

        var tokenIds = new HashSet<string>(StringComparer.Ordinal);
        ValidateTokens(designSystem, tokenIds, diagnostics);
        var componentIds = designSystem.Components.Select(component => component.ComponentId).ToArray();
        ValidateUniqueIds(componentIds, "components", diagnostics);
        var unresolvedTokenIds = designSystem.Components
            .SelectMany(component => component.TokenIds)
            .Concat(designSystem.Typography.Tokens.SelectMany(token => token.AllowedPageTypeIds).Where(_ => false))
            .Where(tokenId => !tokenIds.Contains(tokenId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tokenId => tokenId, StringComparer.Ordinal)
            .ToArray();
        foreach (var tokenId in unresolvedTokenIds)
        {
            AddError("UnknownTokenReference", "components", $"组件引用了未知设计令牌：{tokenId}", diagnostics);
        }

        var pageTypeIds = designSystem.PageTypes.Select(pageType => pageType.PageTypeId).ToArray();
        ValidateUniqueIds(pageTypeIds, "pageTypes", diagnostics);
        var pageTypeSet = pageTypeIds.ToHashSet(StringComparer.Ordinal);
        var componentSet = componentIds.ToHashSet(StringComparer.Ordinal);
        foreach (var pageType in designSystem.PageTypes)
        {
            ValidatePageType(pageType, pageTypeSet, componentSet, expectedSlideIds, diagnostics);
        }

        foreach (var typography in designSystem.Typography.Tokens)
        {
            foreach (var pageTypeId in typography.AllowedPageTypeIds.Where(pageTypeId => !pageTypeSet.Contains(pageTypeId)))
            {
                AddError("UnknownPageTypeReference", $"typography.tokens[{typography.TokenId}].allowedPageTypeIds", $"引用了未知页面类型：{pageTypeId}", diagnostics);
            }
        }

        var unresolvedComponentIds = designSystem.PageTypes
            .SelectMany(pageType => pageType.ComponentIds)
            .Where(componentId => !componentSet.Contains(componentId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(componentId => componentId, StringComparer.Ordinal)
            .ToArray();
        var unresolvedResourceIds = designSystem.AssetPolicy.ResourceRules
            .Select(rule => rule.ResourceId)
            .Where(resourceId => !resources.Contains(resourceId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(resourceId => resourceId, StringComparer.Ordinal)
            .ToArray();
        foreach (var resourceId in unresolvedResourceIds)
        {
            AddError("UnknownResourceReference", "assetPolicy.resourceRules", $"素材策略引用了未知 ResourceId：{resourceId}", diagnostics);
        }

        ValidateAssignments(designSystem.PageTypeAssignments, expectedSlideIds, pageTypeSet, diagnostics);
        ValidateTemplates(designSystem.PageTemplates, pageTypeSet, diagnostics);
        ValidateEvidence(designSystem, expectedSlideIds, resources, visualAnalysisExecuted, diagnostics);
        ValidateAccessibility(designSystem.Accessibility, diagnostics);
        ValidateNoAbsolutePaths(designSystem, diagnostics);

        var assignedSlideIds = designSystem.PageTypeAssignments
            .GroupBy(assignment => assignment.SlideId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1 && expectedSlideIds.Contains(group.Key, StringComparer.Ordinal))
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var uncoveredSlideIds = expectedSlideIds
            .Where(slideId => !assignedSlideIds.Contains(slideId))
            .OrderBy(slideId => slideId, StringComparer.Ordinal)
            .ToArray();
        return new CoursewareDesignSystemValidationReport
        {
            IsValid = diagnostics.All(diagnostic => diagnostic.Severity != CoursewareValidationSeverity.Error),
            CoveredSlideCount = expectedSlideIds.Length - uncoveredSlideIds.Length,
            TotalSlideCount = expectedSlideIds.Length,
            UnresolvedTokenIds = unresolvedTokenIds,
            UnresolvedComponentIds = unresolvedComponentIds,
            UnresolvedResourceIds = unresolvedResourceIds,
            UncoveredSlideIds = uncoveredSlideIds,
            Diagnostics = diagnostics,
        };
    }

    private static void ValidateTokens(
        CoursewareDesignSystem designSystem,
        ISet<string> tokenIds,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        var allTokenIds = designSystem.Spacing.Tokens.Select(token => token.TokenId)
            .Concat(designSystem.Typography.Tokens.Select(token => token.TokenId))
            .Concat(designSystem.Colors.Tokens.Select(token => token.TokenId))
            .Concat(designSystem.Effects.Tokens.Select(token => token.TokenId))
            .ToArray();
        ValidateUniqueIds(allTokenIds, "tokens", diagnostics);
        foreach (var tokenId in allTokenIds.Where(tokenId => !string.IsNullOrWhiteSpace(tokenId)))
        {
            tokenIds.Add(tokenId);
        }

        if (designSystem.Spacing.Tokens.Count == 0 || designSystem.Typography.Tokens.Count == 0 || designSystem.Colors.Tokens.Count == 0)
        {
            AddError("CoreTokensRequired", "tokens", "间距、字体和颜色令牌不能为空。", diagnostics);
        }

        foreach (var token in designSystem.Spacing.Tokens)
        {
            if (token.Value < 0)
            {
                AddError("SpacingValueInvalid", $"spacing.tokens[{token.TokenId}]", "间距值不得为负数。", diagnostics);
            }
        }

        foreach (var token in designSystem.Typography.Tokens)
        {
            if (token.FontSize <= 0 || token.LineHeight < 1 || token.EastAsianFontStack.Count == 0 || token.LatinFontStack.Count == 0)
            {
                AddError("TypographyTokenInvalid", $"typography.tokens[{token.TokenId}]", "字体令牌必须具有有效字体栈、字号和行高。", diagnostics);
            }
        }

        foreach (var token in designSystem.Colors.Tokens)
        {
            if (!HexColorRegex.IsMatch(token.HexValue))
            {
                AddError("ColorValueInvalid", $"colors.tokens[{token.TokenId}].hexValue", "颜色必须使用 #RRGGBB 或 #AARRGGBB。", diagnostics);
            }

            foreach (var backgroundTokenId in token.AllowedBackgroundTokenIds.Where(backgroundTokenId => !string.IsNullOrWhiteSpace(backgroundTokenId)))
            {
                if (!allTokenIds.Contains(backgroundTokenId, StringComparer.Ordinal))
                {
                    AddError("UnknownBackgroundToken", $"colors.tokens[{token.TokenId}].allowedBackgroundTokenIds", $"引用了未知背景令牌：{backgroundTokenId}", diagnostics);
                }
            }
        }
    }

    private static void ValidatePageType(
        CoursewarePageTypeContract pageType,
        IReadOnlySet<string> pageTypeIds,
        IReadOnlySet<string> componentIds,
        IReadOnlyCollection<string> expectedSlideIds,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        var path = $"pageTypes[{pageType.PageTypeId}]";
        RequireText(pageType.PageTypeId, "PageTypeIdRequired", path + ".pageTypeId", diagnostics);
        RequireText(pageType.Name, "PageTypeNameRequired", path + ".name", diagnostics);
        RequireText(pageType.Purpose, "PageTypePurposeRequired", path + ".purpose", diagnostics);
        RequireText(pageType.OverflowStrategy, "PageTypeOverflowRequired", path + ".overflowStrategy", diagnostics);
        if (pageType.DensityRange.Minimum < 0
            || pageType.DensityRange.Maximum > 1
            || pageType.DensityRange.Minimum > pageType.DensityRange.Maximum)
        {
            AddError("DensityRangeInvalid", path + ".densityRange", "密度范围必须位于 0 到 1 且最小值不大于最大值。", diagnostics);
        }

        if (pageType.EvidenceReferences.Count == 0)
        {
            AddError("PageTypeEvidenceRequired", path + ".evidenceReferences", "页面类型必须引用至少一个输入页面证据。", diagnostics);
        }

        foreach (var evidence in pageType.EvidenceReferences)
        {
            if (evidence.SourceKind == CoursewareEvidenceSourceKind.SlideMarkdownFact
                && evidence.SlideId is not null
                && !expectedSlideIds.Contains(evidence.SlideId, StringComparer.Ordinal))
            {
                AddError("UnknownSlideEvidence", path + ".evidenceReferences", $"引用了未知 SlideId：{evidence.SlideId}", diagnostics);
            }
        }

        ValidateUniqueIds(pageType.Slots.Select(slot => slot.SlotId), path + ".slots", diagnostics);
        if (pageType.Slots.Count == 0)
        {
            AddError("PageTypeSlotsRequired", path + ".slots", "页面类型必须定义至少一个内容或素材槽位。", diagnostics);
        }

        foreach (var componentId in pageType.ComponentIds.Where(componentId => !componentIds.Contains(componentId)))
        {
            AddError("UnknownComponentReference", path + ".componentIds", $"引用了未知组件：{componentId}", diagnostics);
        }

        if (pageType.FallbackPageTypeId is not null && !pageTypeIds.Contains(pageType.FallbackPageTypeId))
        {
            AddError("UnknownFallbackPageType", path + ".fallbackPageTypeId", $"引用了未知 fallback 页面类型：{pageType.FallbackPageTypeId}", diagnostics);
        }
    }

    private static void ValidateAssignments(
        IReadOnlyList<CoursewarePageTypeAssignment> assignments,
        IReadOnlyCollection<string> expectedSlideIds,
        IReadOnlySet<string> pageTypeIds,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        foreach (var group in assignments.GroupBy(assignment => assignment.SlideId, StringComparer.Ordinal))
        {
            if (!expectedSlideIds.Contains(group.Key, StringComparer.Ordinal))
            {
                AddError("UnknownAssignedSlide", "pageTypeAssignments", $"页面类型映射包含未知 SlideId：{group.Key}", diagnostics);
            }

            if (group.Count() != 1)
            {
                AddError("DuplicateSlideAssignment", "pageTypeAssignments", $"SlideId 必须且只能映射一次：{group.Key}", diagnostics);
            }
        }

        foreach (var assignment in assignments)
        {
            if (!pageTypeIds.Contains(assignment.PageTypeId))
            {
                AddError("UnknownAssignedPageType", "pageTypeAssignments", $"页面映射引用未知页面类型：{assignment.PageTypeId}", diagnostics);
            }

            if (assignment.Confidence is < 0 or > 1)
            {
                AddError("AssignmentConfidenceInvalid", "pageTypeAssignments", "页面类型映射置信度必须位于 0 到 1。", diagnostics);
            }
        }
    }

    private static void ValidateTemplates(
        IReadOnlyList<CoursewarePageTemplate> templates,
        IReadOnlySet<string> pageTypeIds,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        ValidateUniqueIds(templates.Select(template => template.TemplateId), "pageTemplates", diagnostics);
        var templateIds = templates.Select(template => template.TemplateId).ToHashSet(StringComparer.Ordinal);
        foreach (var template in templates)
        {
            var path = $"pageTemplates[{template.TemplateId}]";
            if (!pageTypeIds.Contains(template.PageTypeId))
            {
                AddError("UnknownTemplatePageType", path + ".pageTypeId", $"模板引用未知页面类型：{template.PageTypeId}", diagnostics);
            }

            if (string.IsNullOrWhiteSpace(template.SlideMlTemplate))
            {
                AddError("SlideMlTemplateRequired", path + ".slideMlTemplate", "模板 SlideML 不能为空。", diagnostics);
            }

            if (template.FallbackTemplateId is not null && !templateIds.Contains(template.FallbackTemplateId))
            {
                AddError("UnknownFallbackTemplate", path + ".fallbackTemplateId", $"引用了未知 fallback 模板：{template.FallbackTemplateId}", diagnostics);
            }
        }
    }

    private static void ValidateEvidence(
        CoursewareDesignSystem designSystem,
        IReadOnlyCollection<string> expectedSlideIds,
        IReadOnlySet<string> resourceIds,
        bool visualAnalysisExecuted,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        ValidateUniqueIds(designSystem.Evidence.Select(evidence => evidence.DecisionId), "evidence", diagnostics);
        ValidateUniqueIds(designSystem.Assumptions.Select(assumption => assumption.AssumptionId), "assumptions", diagnostics);
        var references = designSystem.Evidence.SelectMany(evidence => evidence.Sources)
            .Concat(designSystem.Assumptions.SelectMany(assumption => assumption.Sources))
            .Concat(designSystem.PageTypes.SelectMany(pageType => pageType.EvidenceReferences));
        foreach (var reference in references)
        {
            if (reference.SourceKind == CoursewareEvidenceSourceKind.VisualObservation && !visualAnalysisExecuted)
            {
                AddError("VisualEvidenceWithoutImages", "evidence", "未执行图像分析时不得提交 VisualObservation。", diagnostics);
            }

            if (reference.SlideId is not null && !expectedSlideIds.Contains(reference.SlideId, StringComparer.Ordinal))
            {
                AddError("UnknownEvidenceSlide", "evidence", $"证据引用未知 SlideId：{reference.SlideId}", diagnostics);
            }

            if (reference.ResourceId is not null && !resourceIds.Contains(reference.ResourceId))
            {
                AddError("UnknownEvidenceResource", "evidence", $"证据引用未知 ResourceId：{reference.ResourceId}", diagnostics);
            }
        }
    }

    private static void ValidateAccessibility(
        CoursewareAccessibilityRules rules,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        if (rules.MinimumBodyFontSize <= 0
            || rules.MinimumNormalTextContrastRatio < 1
            || rules.MinimumLargeTextContrastRatio < 1)
        {
            AddError("AccessibilityRulesInvalid", "accessibility", "最小字号和对比度阈值必须为有效正数。", diagnostics);
        }
    }

    private static void ValidateNoAbsolutePaths(
        CoursewareDesignSystem designSystem,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        try
        {
            var json = JsonSerializer.Serialize(designSystem, CoursewareDesignJsonSerializerContext.Default.CoursewareDesignSystem);
            CoursewareAnalysisInputBuilder.ValidateNoAbsolutePaths(json);
        }
        catch (InvalidOperationException)
        {
            AddError("AbsolutePathDetected", "$", "设计系统不得包含本地绝对路径。", diagnostics);
        }
    }

    private static void ValidateUniqueIds(
        IEnumerable<string> ids,
        string path,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        var normalizedIds = ids.ToArray();
        if (normalizedIds.Any(string.IsNullOrWhiteSpace))
        {
            AddError("IdRequired", path, "稳定 ID 不能为空。", diagnostics);
        }

        foreach (var duplicate in normalizedIds
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .GroupBy(id => id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            AddError("DuplicateId", path, $"稳定 ID 重复：{duplicate.Key}", diagnostics);
        }
    }

    private static void RequireText(
        string value,
        string code,
        string path,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(code, path, "必填文本不能为空。", diagnostics);
        }
    }

    private static void RequireEqual(
        string actual,
        string expected,
        string code,
        string path,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            AddError(code, path, $"必须为 {expected}。", diagnostics);
        }
    }

    private static void AddError(
        string code,
        string path,
        string message,
        ICollection<CoursewareValidationDiagnostic> diagnostics)
    {
        diagnostics.Add(new CoursewareValidationDiagnostic
        {
            Code = code,
            Path = path,
            Message = message,
            Severity = CoursewareValidationSeverity.Error,
        });
    }
}
