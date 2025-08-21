using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SimpleWrite.Models;

namespace SimpleWrite.ViewModels;

public class EditorViewModel : ViewModelBase
{
    public EditorViewModel()
    {
        EditorModelList.Add(_currentEditorModel);
    }

    public ObservableCollection<EditorModel> EditorModelList { get; } = [];

    private EditorModel _currentEditorModel = new EditorModel();

    public EditorModel CurrentEditorModel
    {
        get => _currentEditorModel;
        set
        {
            if (Equals(value, _currentEditorModel)) return;
            _currentEditorModel = value;
            OnPropertyChanged();
            EditorModelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? EditorModelChanged;
}