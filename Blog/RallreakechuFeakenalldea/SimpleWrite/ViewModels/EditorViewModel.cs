using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Models;

namespace SimpleWrite.ViewModels;

public class EditorViewModel : ViewModelBase
{
    public EditorViewModel()
    {
        EditorModelList.Add(_currentEditorModel);
    }

    /// <summary>
    /// 当前标记列表，等于标签栏
    /// </summary>
    public ObservableCollection<EditorModel> EditorModelList { get; } = [];

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

    private EditorModel _currentEditorModel = new EditorModel();

    public event EventHandler? EditorModelChanged;

    /// <summary>
    /// 快捷键管理器。这里存放的是数据层，即快捷键绑定方式的数据
    /// </summary>
    internal ShortcutManager ShortcutManager { get; } = new ShortcutManager();
}