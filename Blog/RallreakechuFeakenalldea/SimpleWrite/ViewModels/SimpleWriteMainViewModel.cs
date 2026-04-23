using AvaloniaAgentLib.ViewModel;

using SimpleWrite.Business;
using SimpleWrite.Business.PluginCommandPatterns;
using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.Business.TextEditors.CommandPatterns;
using SimpleWrite.Foundation;
using SimpleWrite.Models;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace SimpleWrite.ViewModels;

public class SimpleWriteMainViewModel : ViewModelBase
{
    public SimpleWriteMainViewModel()
    {
        AppPathManager = new AppPathManager();
        ConfigurationManager = new ConfigurationManager(AppPathManager);

        StatusViewModel = new StatusViewModel()
        {
            MainViewModel = this,
        };

        EditorViewModel = new EditorViewModel()
        {
            MainViewModel = this,
        };

        FolderExplorerViewModel = new FolderExplorerViewModel(this);
        FindReplaceViewModel = new FindReplaceViewModel(EditorViewModel, FolderExplorerViewModel);

        EditorViewModel.EditorModelChanged += (sender, args) =>
        {
            AddSaveStatusChanged(EditorViewModel.CurrentEditorModel);
            FindReplaceViewModel.RefreshCurrentEditor();
        };

        FindReplaceViewModel.PropertyChanged += FindReplaceViewModelOnPropertyChanged;

        AddSaveStatusChanged(EditorViewModel.CurrentEditorModel);
        FindReplaceViewModel.RefreshCurrentEditor();
        StatusViewModel.SetFindStatusText(FindReplaceViewModel.SearchStatusText);

        var pluginCommandPatternProvider = new PluginCommandPatternProvider(this);
        pluginCommandPatternProvider.AddPatterns(CommandPatternManager);

        void AddSaveStatusChanged(EditorModel editorModel)
        {
            StatusViewModel.SaveStatus = editorModel.SaveStatus;

            editorModel.SaveStatusChanged -= EditorModelOnSaveStatusChanged;
            editorModel.SaveStatusChanged += EditorModelOnSaveStatusChanged;

            void EditorModelOnSaveStatusChanged(object? sender, EventArgs e)
            {
                StatusViewModel.SaveStatus = editorModel.SaveStatus;
            }
        }

        void FindReplaceViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FindReplaceViewModel.SearchStatusText))
            {
                StatusViewModel.SetFindStatusText(FindReplaceViewModel.SearchStatusText);
            }
        }
    }

    public AppPathManager AppPathManager { get; }

    public ConfigurationManager ConfigurationManager { get; }

    public StatusViewModel StatusViewModel { get; }
    public EditorViewModel EditorViewModel { get; }
    public FolderExplorerViewModel FolderExplorerViewModel { get; }
    public FindReplaceViewModel FindReplaceViewModel { get; }

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task OpenFileAsync(FileInfo file)
    {
        // 不想直接将 EditorViewModel 给到上层直接调用，所以在这里做一个转发
        await EditorViewModel.OpenFileAsync(file);
    }

    public CommandPatternManager CommandPatternManager { get; } = new();

    internal ISidebarConversationPresenter? SidebarConversationPresenter { get; set; }

    public bool IsCloseConfirmationVisible
    {
        get => _isCloseConfirmationVisible;
        private set => SetField(ref _isCloseConfirmationVisible, value);
    }

    private bool _isCloseConfirmationVisible;

    public string CloseConfirmationTitle
    {
        get => _closeConfirmationTitle;
        private set => SetField(ref _closeConfirmationTitle, value);
    }

    private string _closeConfirmationTitle = string.Empty;

    public string CloseConfirmationDescription
    {
        get => _closeConfirmationDescription;
        private set => SetField(ref _closeConfirmationDescription, value);
    }

    private string _closeConfirmationDescription = string.Empty;

    public string CloseConfirmationFileHint
    {
        get => _closeConfirmationFileHint;
        private set => SetField(ref _closeConfirmationFileHint, value);
    }

    private string _closeConfirmationFileHint = string.Empty;

    public string CloseConfirmationRecoveryHint
    {
        get => _closeConfirmationRecoveryHint;
        private set => SetField(ref _closeConfirmationRecoveryHint, value);
    }

    private string _closeConfirmationRecoveryHint = string.Empty;

    public string CloseConfirmationSaveButtonText
    {
        get => _closeConfirmationSaveButtonText;
        private set => SetField(ref _closeConfirmationSaveButtonText, value);
    }

    private string _closeConfirmationSaveButtonText = "保存并关闭";

    public Task<CloseDocumentDecision> ShowCloseConfirmationAsync(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (_closeConfirmationTaskCompletionSource is not null)
        {
            return Task.FromResult(CloseDocumentDecision.Cancel);
        }

        var displayTitle = string.IsNullOrWhiteSpace(editorModel.Title)
            ? EditorModel.DefaultTitle
            : editorModel.Title;

        if (editorModel.FileInfo is { } fileInfo)
        {
            CloseConfirmationTitle = "关闭前先保存这些修改？";
            CloseConfirmationDescription = $"“{displayTitle}” 还有未写回本地文件的内容，保存后再关闭会更稳妥。";
            CloseConfirmationFileHint = $"本地文件：{fileInfo.FullName}";
            CloseConfirmationSaveButtonText = "保存并关闭";
        }
        else
        {
            CloseConfirmationTitle = "关闭前先保留这份草稿？";
            CloseConfirmationDescription = $"“{displayTitle}” 还没有正式保存到本地文件，现在关闭将只保留临时恢复副本。";
            CloseConfirmationFileHint = "本地文件：尚未关联，选择“保存并关闭”后会进入另存为。";
            CloseConfirmationSaveButtonText = "另存并关闭";
        }

        CloseConfirmationRecoveryHint = "自动恢复：停止编辑 3 秒后会写入 Temp 副本，并仅保留最近 10 个版本。";
        IsCloseConfirmationVisible = true;

        _closeConfirmationTaskCompletionSource = new TaskCompletionSource<CloseDocumentDecision>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _closeConfirmationTaskCompletionSource.Task;
    }

    public void ConfirmCloseBySaving()
    {
        ResolveCloseConfirmation(CloseDocumentDecision.SaveAndClose);
    }

    public void ConfirmCloseWithoutSaving()
    {
        ResolveCloseConfirmation(CloseDocumentDecision.DiscardAndClose);
    }

    public void CancelCloseConfirmation()
    {
        ResolveCloseConfirmation(CloseDocumentDecision.Cancel);
    }

    private void ResolveCloseConfirmation(CloseDocumentDecision decision)
    {
        var taskCompletionSource = Interlocked.Exchange(ref _closeConfirmationTaskCompletionSource, null);
        if (taskCompletionSource is null)
        {
            return;
        }

        IsCloseConfirmationVisible = false;
        taskCompletionSource.TrySetResult(decision);
    }

    private TaskCompletionSource<CloseDocumentDecision>? _closeConfirmationTaskCompletionSource;

    public enum CloseDocumentDecision
    {
        SaveAndClose,
        DiscardAndClose,
        Cancel,
    }
}