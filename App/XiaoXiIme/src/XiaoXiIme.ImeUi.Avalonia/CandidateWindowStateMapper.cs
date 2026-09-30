using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeUi.Avalonia;

public static class CandidateWindowStateMapper
{
    private const int CandidatePageCapacity = 9;

    public static CandidateWindowViewState Map(ImeUiState uiState)
    {
        ArgumentNullException.ThrowIfNull(uiState);

        if (!uiState.CandidateWindowVisible || uiState.Candidates.Count == 0)
        {
            return CandidateWindowViewState.Hidden with
            {
                CompositionText = uiState.Composition.DisplayText,
                GuidelineText = uiState.Guideline.Text,
                AnchorX = uiState.AnchorX,
                AnchorY = uiState.AnchorY,
                AttributionText = uiState.EffectiveAbout.Notice,
                DiagnosticText = uiState.DiagnosticText ?? string.Empty,
            };
        }

        var candidateCount = uiState.Candidates.Count;
        var pageSize = NormalizePageSize(uiState.CandidateWindow.PageSize, candidateCount);
        var selection = Math.Clamp(uiState.CandidateWindow.Selection, 0, candidateCount - 1);
        var pageStart = NormalizePageStart(uiState.CandidateWindow.PageStart, selection, candidateCount);
        pageSize = Math.Min(pageSize, candidateCount - pageStart);
        var pageEnd = pageStart + pageSize;
        var candidates = new List<CandidateWindowCandidateViewModel>(pageEnd - pageStart);

        for (var index = pageStart; index < pageEnd; index++)
        {
            var candidate = uiState.Candidates[index];
            candidates.Add(new CandidateWindowCandidateViewModel(
                DisplayIndex: index - pageStart + 1,
                CandidateIndex: index,
                candidate.Text,
                candidate.Reading,
                IsSelected: index == selection));
        }

        return new CandidateWindowViewState(
            IsVisible: true,
            uiState.Composition.DisplayText,
            candidates,
            selection,
            pageStart,
            pageSize,
            CurrentPage: (pageStart / CandidatePageCapacity) + 1,
            TotalPages: (candidateCount + CandidatePageCapacity - 1) / CandidatePageCapacity,
            uiState.Guideline.Text,
            uiState.AnchorX,
            uiState.AnchorY,
            uiState.EffectiveAbout.Notice,
            uiState.DiagnosticText ?? string.Empty);
    }

    private static int NormalizePageSize(int pageSize, int candidateCount)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        if (pageSize <= 0)
        {
            return Math.Min(CandidatePageCapacity, candidateCount);
        }

        return Math.Min(pageSize, candidateCount);
    }

    private static int NormalizePageStart(int pageStart, int selection, int candidateCount)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        if (selection < pageStart || selection >= pageStart + CandidatePageCapacity)
        {
            pageStart = (selection / CandidatePageCapacity) * CandidatePageCapacity;
        }

        var lastPageStart = ((candidateCount - 1) / CandidatePageCapacity) * CandidatePageCapacity;
        return Math.Clamp(pageStart, 0, lastPageStart);
    }
}
