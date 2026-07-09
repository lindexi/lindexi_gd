using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeUi.Avalonia;

public static class CandidateWindowStateMapper
{
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
            };
        }

        var candidateCount = uiState.Candidates.Count;
        var pageSize = NormalizePageSize(uiState.CandidateWindow.PageSize, candidateCount);
        var selection = Math.Clamp(uiState.CandidateWindow.Selection, 0, candidateCount - 1);
        var pageStart = NormalizePageStart(uiState.CandidateWindow.PageStart, selection, pageSize, candidateCount);
        var pageEnd = Math.Min(candidateCount, pageStart + pageSize);
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
            CurrentPage: (pageStart / pageSize) + 1,
            TotalPages: (candidateCount + pageSize - 1) / pageSize,
            uiState.Guideline.Text,
            uiState.AnchorX,
            uiState.AnchorY);
    }

    private static int NormalizePageSize(int pageSize, int candidateCount)
    {
        if (candidateCount <= 0)
        {
            return 0;
        }

        if (pageSize <= 0)
        {
            return Math.Min(9, candidateCount);
        }

        return Math.Min(pageSize, candidateCount);
    }

    private static int NormalizePageStart(int pageStart, int selection, int pageSize, int candidateCount)
    {
        if (pageSize <= 0 || candidateCount <= 0)
        {
            return 0;
        }

        if (selection < pageStart || selection >= pageStart + pageSize)
        {
            pageStart = (selection / pageSize) * pageSize;
        }

        var lastPageStart = ((candidateCount - 1) / pageSize) * pageSize;
        return Math.Clamp(pageStart, 0, lastPageStart);
    }
}
