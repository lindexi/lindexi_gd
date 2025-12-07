using System;

using SimpleWrite.Models;

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
}