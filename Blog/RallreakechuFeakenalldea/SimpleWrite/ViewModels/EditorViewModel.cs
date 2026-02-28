using Avalonia.Controls;

using LightTextEditorPlus;

using SimpleWrite.Business.FileHandlers;
using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Business.Snippets;
using SimpleWrite.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWrite.ViewModels;

public class EditorViewModel : ViewModelBase
{
    public EditorViewModel()
    {
        if (Design.IsDesignMode)
        {
            EditorModelList.Add(new EditorModel()
            {
                Title = "文档1",
                FileInfo = new FileInfo(@"C:\Document\Text1.txt"),
            });

            EditorModelList.Add(new EditorModel()
            {
                Title = "文档a",
                FileInfo = new FileInfo(@"C:\Document\Text2.txt"),
            });
        }

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
    /// 代码片管理器
    /// </summary>
    public SnippetManager SnippetManager { get; } = new SnippetManager();

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

        var newEditorModel = new EditorModel
        {
            FileInfo = file
        };
        EditorModelList.Add(newEditorModel);
        CurrentEditorModel = newEditorModel;

        if (originEditorModel.IsEmptyText())
        {
            // 如果原本就是空文本，则删除当前的内容
            EditorModelList.Remove(originEditorModel);
        }

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
        if (FilePickerHandler is null)
        {
            SetSaveStatus(SaveStatus.Error);
            return;
        }

        var saveFile = await FilePickerHandler.PickSaveFileAsync();
        if (saveFile is null)
        {
            SetSaveStatus(SaveStatus.Error);
            return;
        }

        CurrentEditorModel.FileInfo = saveFile;
        await SaveEditorModelToFileAsync(CurrentEditorModel, saveFile);
        SetSaveStatus(SaveStatus.Saved);
    }

    /// <summary>
    /// 打开文档
    /// </summary>
    public async Task OpenDocumentAsync()
    {
        if (FilePickerHandler is null)
        {
            return;
        }

        var openFile = await FilePickerHandler.PickOpenFileAsync();
        if (openFile is null)
        {
            return;
        }

        await OpenFileAsync(openFile);
    }

    /// <summary>
    /// 新建文档
    /// </summary>
    public void NewDocument()
    {
        if (CurrentEditorModel.IsEmptyText())
        {
            return;
        }

        var newEditorModel = new EditorModel();
        EditorModelList.Add(newEditorModel);
        CurrentEditorModel = newEditorModel;
    }

    /// <summary>
    /// 关闭文档
    /// </summary>
    public void CloseDocument(EditorModel editorModel)
    {
        var currentIndex = EditorModelList.IndexOf(editorModel);
        if (currentIndex < 0)
        {
            return;
        }

        if (EditorModelList.Count <= 1)
        {
            // 只剩下一个，那就需要先加再删除，防止传入空值
            var emptyEditorModel = new EditorModel();
            EditorModelList.Add(emptyEditorModel);
            // 需要先加入到 EditorModelList 列表再设置值，否则将会导致不在列表中而让框架设置空
            CurrentEditorModel = emptyEditorModel;

            EditorModelList.RemoveAt(currentIndex);
        }
        else
        {
            var nextIndex = Math.Clamp(currentIndex - 1, 0, EditorModelList.Count - 1);
            if (nextIndex == currentIndex)
            {
                // 既然 -1 拿到自身，如第 0 个，那就试试取右边一个
                nextIndex = Math.Clamp(currentIndex + 1, 0, EditorModelList.Count - 1);
            }

            // 需要先给 CurrentEditorModel 赋值，防止被删除后设置空值
            CurrentEditorModel = EditorModelList[nextIndex];
            EditorModelList.RemoveAt(currentIndex);
        }
    }

    /// <summary>
    /// 关闭当前文档
    /// </summary>
    public void CloseCurrentDocument()
    {
        if (EditorModelList.Count <= 1)
        {
            if (!CurrentEditorModel.IsEmptyText())
            {
                CurrentEditorModel = new EditorModel();
                EditorModelList.Clear();
                EditorModelList.Add(CurrentEditorModel);
            }

            return;
        }

        var currentIndex = EditorModelList.IndexOf(CurrentEditorModel);
        if (currentIndex < 0)
        {
            return;
        }

        EditorModelList.RemoveAt(currentIndex);
        var nextIndex = Math.Clamp(currentIndex, 0, EditorModelList.Count - 1);
        CurrentEditorModel = EditorModelList[nextIndex];
    }

    /// <summary>
    /// 切换到下一个文档
    /// </summary>
    public void SwitchToNextDocument()
    {
        if (EditorModelList.Count <= 1)
        {
            return;
        }

        var currentIndex = EditorModelList.IndexOf(CurrentEditorModel);
        if (currentIndex < 0)
        {
            return;
        }

        var nextIndex = (currentIndex + 1) % EditorModelList.Count;
        CurrentEditorModel = EditorModelList[nextIndex];
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

    internal IFilePickerHandler? FilePickerHandler { get; set; }

    private void SetSaveStatus(SaveStatus saveStatus)
    {
        //if (StatusViewModel != null)
        //{
        //    StatusViewModel.IsSaving = saveStatus;
        //}

        CurrentEditorModel.SaveStatus = saveStatus;
    }
}