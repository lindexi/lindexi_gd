using SimpleWrite.Models;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using AvaloniaAgentLib.ViewModel;
using SimpleWrite.Business;
using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.Foundation;

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

        FindReplaceViewModel = new FindReplaceViewModel(EditorViewModel);

        EditorViewModel.EditorModelChanged += (sender, args) =>
        {
            AddSaveStatusChanged(EditorViewModel.CurrentEditorModel);
            FindReplaceViewModel.RefreshCurrentEditor();
        };

        FindReplaceViewModel.PropertyChanged += FindReplaceViewModelOnPropertyChanged;

        AddSaveStatusChanged(EditorViewModel.CurrentEditorModel);
        FindReplaceViewModel.RefreshCurrentEditor();
        StatusViewModel.SetFindStatusText(FindReplaceViewModel.SearchStatusText);

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

    public Task SendMessageToCopilotAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        return CopilotHandler?.SendMessageToCopilotAsync(text, withHistory:false) ?? Task.CompletedTask;
    }

    public ICopilotHandler? CopilotHandler { get; set; }
}