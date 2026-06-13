using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JedicairqaiWejobajaibiqe;

/// <summary>
/// 用于测试 ItemsControl 滚动行为的数据项。
/// </summary>
public class TextItem : INotifyPropertyChanged
{
    private string _content = string.Empty;

    /// <summary>
    /// 显示的文本内容。
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
