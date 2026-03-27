using SimpleWrite.Models;

using System;
using Avalonia.Controls;
using Avalonia.Threading;

namespace SimpleWrite.ViewModels;

public class StatusViewModel : ViewModelBase
{
    public StatusViewModel()
    {
        if (Design.IsDesignMode)
        {
            _currentCaretInfoText = "光标: 9999, 段:100, 行:100,字符:0('v' 0x75)";
            _saveStatus = SaveStatus.Saving;
            _findStatusText = "[查找: 5 项，第 2 项]";
        }
    }

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

            var findStatusText = string.IsNullOrEmpty(_findStatusText) ? string.Empty : $" {_findStatusText}";
            return $"{savingText}{findStatusText} {_currentCaretInfoText ?? string.Empty}";
        }
    }

    public void SetFindStatusText(string? findStatusText)
    {
        if (findStatusText == _findStatusText)
        {
            return;
        }

        _findStatusText = findStatusText;
        OnPropertyChanged(nameof(StatusText));
    }

    public void SetCurrentCaretInfoText(string currentCaretInfoText)
    {
        _currentCaretInfoText = currentCaretInfoText;

        // 放在后台执行，防止在主线程访问 Render 之后，执行强行渲染导致布局完成更新光标信息，从而触发 Avalonia 已知问题： 禁止在 Render 时执行 InvalidVisual 方法
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(StatusText));
        }, DispatcherPriority.Background);
    }

    private string? _currentCaretInfoText;
    private string? _findStatusText;
}