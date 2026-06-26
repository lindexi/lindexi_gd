using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using LightTextEditorPlus;

using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.ViewModels;

/// <summary>
/// 浮动命令面板的 ViewModel，管理搜索过滤、选中项与可见性。
/// </summary>
public class CommandPaletteViewModel : ViewModelBase
{
    private bool _isVisible;
    private string _searchText = string.Empty;
    private CommandPaletteItem? _selectedItem;
    private string _selectedText = string.Empty;
    private List<CommandPaletteItem> _matchedCommands = [];

    /// <summary>
    /// 获取或设置命令面板是否可见。
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    /// <summary>
    /// 获取或设置搜索关键词。设置时触发客户端过滤。
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            FilterCommands();
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 获取过滤后的命令列表。
    /// </summary>
    public ObservableCollection<CommandPaletteItem> FilteredCommands { get; } = [];

    /// <summary>
    /// 获取或设置当前选中的命令项。
    /// </summary>
    public CommandPaletteItem? SelectedItem
    {
        get => _selectedItem;
        set => SetField(ref _selectedItem, value);
    }

    /// <summary>
    /// 打开命令面板，根据选中文本匹配可用命令。
    /// </summary>
    /// <param name="selectedText">当前选中的文本。</param>
    /// <param name="manager">命令模式管理器，可为 null。</param>
    /// <param name="isSingleLine">是否为单行上下文（无选区时取当前段落文本），为 true 时跳过不支持单行的命令。</param>
    public async Task OpenAsync(string selectedText, CommandPatternManager? manager, bool isSingleLine)
    {
        _selectedText = selectedText;
        SearchText = string.Empty;

        if (manager is null)
        {
            _matchedCommands = [];
        }
        else
        {
            _matchedCommands = new List<CommandPaletteItem>(manager.CommandPatternList.Count);
            foreach (ICommandPattern pattern in manager.CommandPatternList)
            {
                if (isSingleLine && !pattern.SupportSingleLine)
                {
                    continue;
                }

                bool isMatch = await pattern.IsMatchAsync(selectedText);
                if (isMatch)
                {
                    _matchedCommands.Add(new CommandPaletteItem(pattern, IsMatch: true));
                }
            }
        }

        FilteredCommands.Clear();
        foreach (CommandPaletteItem item in _matchedCommands)
        {
            FilteredCommands.Add(item);
        }

        SelectedItem = FilteredCommands.FirstOrDefault();
        IsVisible = true;
    }

    /// <summary>
    /// 关闭命令面板并重置状态。
    /// </summary>
    public void Close()
    {
        IsVisible = false;
        SearchText = string.Empty;
        FilteredCommands.Clear();
        _matchedCommands = [];
    }

    /// <summary>
    /// 执行当前选中的命令。
    /// </summary>
    /// <param name="textEditor">目标文本编辑器。</param>
    public async Task ExecuteSelectedAsync(TextEditor textEditor)
    {
        if (SelectedItem is not null && !string.IsNullOrEmpty(_selectedText))
        {
            IsVisible = false;
            await SelectedItem.Pattern.DoAsync(_selectedText, textEditor);
        }
    }

    /// <summary>
    /// 根据搜索关键词对已匹配命令进行客户端过滤。
    /// </summary>
    private void FilterCommands()
    {
        FilteredCommands.Clear();
        foreach (CommandPaletteItem item in _matchedCommands)
        {
            if (string.IsNullOrWhiteSpace(SearchText) ||
                item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                FilteredCommands.Add(item);
            }
        }

        SelectedItem = FilteredCommands.FirstOrDefault();
    }
}
