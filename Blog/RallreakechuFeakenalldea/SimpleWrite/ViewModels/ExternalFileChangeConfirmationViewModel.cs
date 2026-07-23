using SimpleWrite.Models;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWrite.ViewModels;

/// <summary>
/// 管理外部文件变更确认面板的显示状态和用户决策。
/// </summary>
public sealed class ExternalFileChangeConfirmationViewModel : ViewModelBase
{
    private TaskCompletionSource<Decision>? _decisionTaskCompletionSource;
    private bool _isVisible;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _fileHint = string.Empty;

    /// <summary>
    /// 获取确认面板是否可见。
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        private set => SetField(ref _isVisible, value);
    }

    /// <summary>
    /// 获取确认面板标题。
    /// </summary>
    public string Title
    {
        get => _title;
        private set => SetField(ref _title, value);
    }

    /// <summary>
    /// 获取确认面板说明。
    /// </summary>
    public string Description
    {
        get => _description;
        private set => SetField(ref _description, value);
    }

    /// <summary>
    /// 获取发生变更的本地文件提示。
    /// </summary>
    public string FileHint
    {
        get => _fileHint;
        private set => SetField(ref _fileHint, value);
    }

    /// <summary>
    /// 显示确认面板并等待用户选择。
    /// </summary>
    /// <param name="editorModel">发生磁盘文件变更的标签模型。</param>
    /// <returns>用户选择的文件内容处理方式。</returns>
    public Task<Decision> ShowAsync(EditorModel editorModel)
    {
        ArgumentNullException.ThrowIfNull(editorModel);

        if (_decisionTaskCompletionSource is not null)
        {
            return Task.FromResult(Decision.Ignore);
        }

        var displayTitle = string.IsNullOrWhiteSpace(editorModel.Title)
            ? EditorModel.DefaultTitle
            : editorModel.Title;
        var filePath = editorModel.FileInfo?.FullName ?? string.Empty;

        Title = "本地文件已发生变更";
        Description = $"“{displayTitle}”对应的文件已被其他程序修改。采用本地磁盘内容会放弃当前编辑器中的更改；忽略后不会覆盖磁盘文件，也不会放弃当前更改。";
        FileHint = $"本地文件：{filePath}";
        IsVisible = true;

        _decisionTaskCompletionSource = new TaskCompletionSource<Decision>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _decisionTaskCompletionSource.Task;
    }

    /// <summary>
    /// 采用本地磁盘文件内容并放弃编辑器中的当前更改。
    /// </summary>
    public void ReloadFromDisk()
    {
        Resolve(Decision.ReloadFromDisk);
    }

    /// <summary>
    /// 忽略本次本地磁盘文件变更。
    /// </summary>
    public void Ignore()
    {
        Resolve(Decision.Ignore);
    }

    private void Resolve(Decision decision)
    {
        var taskCompletionSource = Interlocked.Exchange(ref _decisionTaskCompletionSource, null);
        if (taskCompletionSource is null)
        {
            return;
        }

        IsVisible = false;
        taskCompletionSource.TrySetResult(decision);
    }

    /// <summary>
    /// 表示用户对磁盘文件变更的处理选择。
    /// </summary>
    public enum Decision
    {
        /// <summary>
        /// 使用磁盘文件内容。
        /// </summary>
        ReloadFromDisk,

        /// <summary>
        /// 保留编辑器当前内容并忽略磁盘变更。
        /// </summary>
        Ignore,
    }
}
