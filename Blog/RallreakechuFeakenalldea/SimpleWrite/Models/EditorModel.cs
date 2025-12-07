using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;

namespace SimpleWrite.Models;

public class EditorModel : INotifyPropertyChanged
{
    /// <summary>
    /// 标题内容
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            if (value == _title) return;
            _title = value;
            OnPropertyChanged();
        }
    }

    private string _title = DefaultTitle;

    public const string DefaultTitle = "无标题";

    [MaybeNull]
    public FileInfo FileInfo
    {
        get => _fileInfo;
        set
        {
            if (Equals(value, _fileInfo)) return;
            _fileInfo = value;
            Title = value.Name;
            OnPropertyChanged();
        }
    }
    private FileInfo? _fileInfo;

    public TextEditor? TextEditor { get; set; }

    public SaveStatus SaveStatus
    {
        get => _saveStatus;
        set
        {
            if (value == _saveStatus) return;
            _saveStatus = value;
            SaveStatusChanged?.Invoke(this,EventArgs.Empty);
            OnPropertyChanged();
        }
    }
    private SaveStatus _saveStatus;

    public event EventHandler? SaveStatusChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
