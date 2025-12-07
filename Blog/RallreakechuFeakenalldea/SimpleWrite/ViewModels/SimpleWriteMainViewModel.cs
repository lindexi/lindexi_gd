using SimpleWrite.Models;

using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleWrite.ViewModels;

public class SimpleWriteMainViewModel
{
    public SimpleWriteMainViewModel()
    {
        StatusViewModel = new StatusViewModel()
        {
            MainViewModel = this,
        };

        EditorViewModel = new EditorViewModel()
        {
            MainViewModel = this,
        };

        EditorViewModel.EditorModelChanged += (sender, args) =>
        {
            AddSaveStatusChanged(EditorViewModel.CurrentEditorModel);
        };

        AddSaveStatusChanged(EditorViewModel.CurrentEditorModel);

        void AddSaveStatusChanged(EditorModel editorModel)
        {
            editorModel.SaveStatusChanged -= EditorModelOnSaveStatusChanged;
            editorModel.SaveStatusChanged += EditorModelOnSaveStatusChanged;

            void EditorModelOnSaveStatusChanged(object? sender, EventArgs e)
            {
                StatusViewModel.IsSaving = editorModel.SaveStatus;
            }
        }
    }

    public StatusViewModel StatusViewModel { get; }
    public EditorViewModel EditorViewModel { get; }

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
}