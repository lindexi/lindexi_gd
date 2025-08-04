using SimpleWrite.Models;

using System;

namespace SimpleWrite.ViewModels;

public class StatusViewModel : ViewModelBase
{
    public SaveStatus IsSaving
    {
        get => _isSaving;
        set
        {
            if (value == _isSaving) return;
            _isSaving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private SaveStatus _isSaving = SaveStatus.Draft;

    public string StatusText
    {
        get
        {
            var savingText = _isSaving switch
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