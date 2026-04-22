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
using System.Threading.Tasks;
namespace SimpleWrite.ViewModels;

public class SimpleWriteMainViewModel
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
}