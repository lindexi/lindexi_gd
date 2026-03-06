using SimpleWrite.Models;

using System;

namespace SimpleWrite.ViewModels;

public class StatusViewModel : ViewModelBase
{
    public SimpleWriteMainViewModel? MainViewModel { get; init; }

    public SaveStatus SaveStatus
    {
        get => _saveStatus;
        set
        {
            if (value == _saveStatus) return;
            _saveStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private SaveStatus _saveStatus = SaveStatus.Draft;

    public string StatusText
    {
        get
        {
            var savingText = _saveStatus switch
            {
                SaveStatus.Draft => "[编辑中]",
                SaveStatus.Saving => "[保存中]",
                SaveStatus.Saved => "[已保存]",
                SaveStatus.Error => "[错误]",
                _ => throw new ArgumentOutOfRangeException()
            };

            return $"{savingText} {_currentCaretInfoText ?? string.Empty}";
        }
    }

    public void SetCurrentCaretInfoText(string currentCaretInfoText)
    {
        _currentCaretInfoText = currentCaretInfoText;
        OnPropertyChanged(nameof(StatusText));
    }

    private string? _currentCaretInfoText;
}