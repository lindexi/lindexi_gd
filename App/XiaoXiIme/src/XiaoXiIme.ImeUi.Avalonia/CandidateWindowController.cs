using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeUi.Avalonia;

public sealed class CandidateWindowController
{
    public CandidateWindowViewState CurrentState { get; private set; } = CandidateWindowViewState.Hidden;

    public CandidateWindowViewState Update(ImeUiState uiState)
    {
        CurrentState = CandidateWindowStateMapper.Map(uiState);
        return CurrentState;
    }

    public CandidateWindowViewState Hide()
    {
        CurrentState = CandidateWindowViewState.Hidden;
        return CurrentState;
    }
}
