using System;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>表示存档页面中的工作任务。</summary>
public sealed class ArchivedWorkTaskItemViewModel
{
    internal ArchivedWorkTaskItemViewModel(WorkTaskRecord record)
    {
        Record = record;
    }

    /// <summary>获取任务标识。</summary>
    public Guid Id => Record.Id;
    /// <summary>获取任务名称。</summary>
    public string DisplayName => Record.DisplayName;
    /// <summary>获取工作目录。</summary>
    public string WorkspacePath => Record.WorkspacePath ?? string.Empty;
    /// <summary>获取模型名称。</summary>
    public string ModelReference => Record.ModelReference ?? string.Empty;

    internal WorkTaskRecord Record { get; }
}
