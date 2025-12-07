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

    public bool IsEmptyText()
    {
        if (_fileInfo is not null)
        {
            return false;
        }

        if (TextEditor is null)
        {
            return true;
        }

        // 判断空文本的方法是： 如果只有一段，且此段没有字符，则是空文本
        // 如果超过一段了，则一定不是空文本
        // 文本库规定，至少存在一段
        var paragraphList = TextEditor.TextEditorCore.ParagraphList;
        if (paragraphList.Count == 1)
        {
            if (paragraphList[0].CharCount == 0)
            {
                return true;
            }
        }

        return false;
    }
}
