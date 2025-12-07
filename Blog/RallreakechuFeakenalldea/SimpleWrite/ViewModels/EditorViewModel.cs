using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.FileHandlers;
using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Models;

namespace SimpleWrite.ViewModels;

public class EditorViewModel : ViewModelBase
{
    public EditorViewModel()
    {
        EditorModelList.Add(_currentEditorModel);

        ShortcutManagerHelper.AddDefaultShortcut(this);
    }

    public SimpleWriteMainViewModel? MainViewModel { get; init; }

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

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public async Task OpenFileAsync(FileInfo file)
    {
        // 如果已经有打开了，就切换过去
        foreach (var editorModel in EditorModelList)
        {
            if (editorModel.FileInfo?.FullName == file.FullName)
            {
                CurrentEditorModel = editorModel;
                return;
            }
        }

        // 如果没有，则打开
        var originEditorModel = CurrentEditorModel;
        if (originEditorModel.IsEmptyText())
        {
            // 如果原本就是空文本，则删除当前的内容
            EditorModelList.Remove(originEditorModel);
        }

        var newEditorModel = new EditorModel
        {
            FileInfo = file
        };
        EditorModelList.Add(newEditorModel);
        CurrentEditorModel = newEditorModel;

        await Task.CompletedTask;
    }

    public async Task LoadFileToTextEditorAsync(EditorModel editorModel, TextEditor textEditor, FileInfo fileInfo)
    {
        if (!ReferenceEquals(editorModel.FileInfo, fileInfo))
        {
            // 传入的参数只是为了解决可空而已
            throw new ArgumentException();
        }

        if (!ReferenceEquals(editorModel.TextEditor, textEditor))
        {
            // 传入的参数只是为了解决可空而已
            throw new ArgumentException();
        }

        await using var fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var streamReader = new StreamReader(fileStream, Encoding.UTF8, leaveOpen: true);
        textEditor.Text = await streamReader.ReadToEndAsync();
    }

    /// <summary>
    /// 保存当前文档
    /// </summary>
    public async Task SaveDocument()
    {
        SetSaveStatus(SaveStatus.Saving);

        if (CurrentEditorModel.FileInfo is null)
        {
            // 尚未保存过，执行另存为逻辑
            await SaveDocumentAs();
        }
        else
        {
            await SaveEditorModelToFileAsync(CurrentEditorModel, CurrentEditorModel.FileInfo);
            SetSaveStatus(SaveStatus.Saved);
        }
    }

    /// <summary>
    /// 当前文档另存为
    /// </summary>
    public async Task SaveDocumentAs()
    {
        SetSaveStatus(SaveStatus.Saving);
        if (SaveFilePickerHandler is null)
        {
            SetSaveStatus(SaveStatus.Error);
            return;
        }

        var saveFile = await SaveFilePickerHandler.PickSaveFileAsync();
        if (saveFile is null)
        {
            SetSaveStatus(SaveStatus.Error);
            return;
        }

        CurrentEditorModel.FileInfo = saveFile;
        await SaveEditorModelToFileAsync(CurrentEditorModel, saveFile);
        SetSaveStatus(SaveStatus.Saved);
    }

    private async Task SaveEditorModelToFileAsync(EditorModel editorModel, FileInfo saveFile)
    {
        if (editorModel.TextEditor is { } textEditor)
        {
            var allText = textEditor.Text;

            if (!ReferenceEquals(editorModel.FileInfo, saveFile))
            {
                // 要求必定是一样的。传入的这个参数只是为了减少可空判断而已
                throw new ArgumentException();
            }

            await using var fileStream = saveFile.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
            await using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8, leaveOpen: true);
            await streamWriter.WriteAsync(allText);
        }
        else
        {
            // 如果还没初始化，证明啥都没有干，那自然就不需要保存
        }
    }

    internal ISaveFilePickerHandler? SaveFilePickerHandler { get; set; }

    private void SetSaveStatus(SaveStatus saveStatus)
    {
        //if (StatusViewModel != null)
        //{
        //    StatusViewModel.IsSaving = saveStatus;
        //}

        CurrentEditorModel.SaveStatus = saveStatus;
    }
}