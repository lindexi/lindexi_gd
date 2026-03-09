using SimpleWrite.Models;

using System;
using Avalonia.Threading;

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

        // 放在后台执行，防止在主线程访问 Render 之后，执行强行渲染导致布局完成更新光标信息，从而触发 Avalonia 已知问题： 禁止在 Render 时执行 InvalidVisual 方法
        _ = Dispatcher.UIThread.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(StatusText));
        }, DispatcherPriority.Background);
    }

    private string? _currentCaretInfoText;
}