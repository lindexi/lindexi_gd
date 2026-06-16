using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Threading;

using Microsoft.Extensions.AI;

using SimpleWrite.Models;
using SimpleWrite.ViewModels;

namespace SimpleWrite.Business.AgentConnectors;

/// <summary>
/// 为 AI 提供标签页相关工具：列出打开标签和读取标签内容。
/// </summary>
public sealed class EditorTabToolProvider
{
    private const int DefaultMaxLines = 400;
    private const int DefaultMaxResults = 100;

    private readonly EditorViewModel _editorViewModel;

    public EditorTabToolProvider(EditorViewModel editorViewModel)
    {
        _editorViewModel = editorViewModel ?? throw new ArgumentNullException(nameof(editorViewModel));
    }

    /// <summary>
    /// 创建供 AI 使用的工具列表。
    /// </summary>
    public IReadOnlyList<AITool> CreateTools()
    {
        return
        [
            AIFunctionFactory.Create(ListOpenTabs, name: nameof(ListOpenTabs),
                description: "列出所有打开的标签页，包含行数。当前标签页若有选中文本，则会显示选中的字符数。"),
            AIFunctionFactory.Create(ReadTabContent, name: nameof(ReadTabContent),
                description: "读取指定标签页的内容。不传 tabId 则读取当前标签页。支持行范围截取，最多 400 行。"),
            AIFunctionFactory.Create(ReadSelectedText, name: nameof(ReadSelectedText),
                description: "获取指定标签页当前选中的文本内容。不传 tabId 则读取当前标签页。"),
        ];
    }

    [Description("列出所有打开的标签页，包含行数。当前标签页若有选中文本，则会显示选中的字符数。")]
    public async Task<string> ListOpenTabs(
        [Description("最多返回多少个标签，默认 100。")] int maxResults = DefaultMaxResults)
    {
        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        var editorModelList = _editorViewModel.EditorModelList;
        var currentEditorModel = _editorViewModel.CurrentEditorModel;

        var tabInfos = new List<TabListInfo>(Math.Min(editorModelList.Count, maxResults));
        for (int i = 0; i < editorModelList.Count && tabInfos.Count < maxResults; i++)
        {
            EditorModel model = editorModelList[i];
            string tabId = GenerateTabId(model);
            int lineCount = await GetLineCountAsync(model);
            int selectionLength = await GetSelectionLengthAsync(model);

            tabInfos.Add(new TabListInfo(
                TabId: tabId,
                Title: model.Title,
                FilePath: model.FileInfo?.FullName,
                LineCount: lineCount,
                IsCurrent: ReferenceEquals(model, currentEditorModel),
                SelectionLength: selectionLength));
        }

        return FormatTabList(tabInfos);
    }

    [Description("读取指定标签页的内容。不传 tabId 则读取当前标签页。支持行范围截取，最多 400 行。")]
    public async Task<string> ReadTabContent(
        [Description("标签标识。不传则读取当前标签页。")] string? tabId = null,
        [Description("起始行号，1-based，默认 1。")] int startLine = 1,
        [Description("结束行号，1-based，包含该行，默认起始行+400。")] int endLine = -1,
        [Description("是否包含行号，默认 true。")] bool includeLineNumbers = true)
    {
        if (startLine < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(startLine), "起始行号必须 >= 1。");
        }

        EditorModel editorModel = ResolveEditorModel(tabId);

        string text = await GetTextAsync(editorModel);
        var lines = text.Split('\n');
        int totalLines = lines.Length;

        if (endLine < 0)
        {
            endLine = startLine + DefaultMaxLines - 1;
        }

        if (endLine < startLine)
        {
            throw new ArgumentOutOfRangeException(nameof(endLine), "结束行号必须 >= 起始行号。");
        }

        int maxEndLine = startLine + DefaultMaxLines - 1;
        if (endLine > maxEndLine)
        {
            endLine = maxEndLine;
        }

        if (startLine > totalLines)
        {
            return $"标签 \"{editorModel.Title}\" 共 {totalLines} 行，起始行号 {startLine} 超出范围。";
        }

        int actualEndLine = Math.Min(endLine, totalLines);

        var sb = new StringBuilder();
        if (includeLineNumbers)
        {
            for (int i = startLine - 1; i < actualEndLine; i++)
            {
                sb.Append($"{i + 1}: ");
                sb.AppendLine(lines[i]);
            }
        }
        else
        {
            for (int i = startLine - 1; i < actualEndLine; i++)
            {
                sb.AppendLine(lines[i]);
            }
        }

        if (actualEndLine < totalLines)
        {
            sb.AppendLine($"(共 {totalLines} 行，以上为第 {startLine}-{actualEndLine} 行)");
        }

        return sb.ToString();
    }

    [Description("获取指定标签页当前选中的文本内容。不传 tabId 则读取当前标签页。")]
    public async Task<string> ReadSelectedText(
        [Description("标签标识。不传则读取当前标签页。")] string? tabId = null)
    {
        EditorModel editorModel = ResolveEditorModel(tabId);
        string text = await GetSelectedTextAsync(editorModel);

        if (string.IsNullOrEmpty(text))
        {
            return $"标签 \"{editorModel.Title}\" 当前没有选中文本。";
        }

        return text;
    }

    private EditorModel ResolveEditorModel(string? tabId)
    {
        if (string.IsNullOrWhiteSpace(tabId))
        {
            return _editorViewModel.CurrentEditorModel;
        }

        foreach (EditorModel model in _editorViewModel.EditorModelList)
        {
            if (string.Equals(GenerateTabId(model), tabId, StringComparison.Ordinal))
            {
                return model;
            }
        }

        throw new ArgumentException($"未找到标签 \"{tabId}\"。请先使用 ListOpenTabs 获取有效的标签标识。", nameof(tabId));
    }

    private string GenerateTabId(EditorModel model)
    {
        // 使用 Title + 重名后缀策略生成对 AI 友好的标签 ID
        string title = model.Title;
        int sameTitleCount = 0;
        bool foundSelf = false;

        foreach (EditorModel item in _editorViewModel.EditorModelList)
        {
            if (ReferenceEquals(item, model))
            {
                foundSelf = true;
                break;
            }

            if (string.Equals(item.Title, title, StringComparison.Ordinal))
            {
                sameTitleCount++;
            }
        }

        if (!foundSelf)
        {
            // model 不在列表中，回退到 Guid
            return title;
        }

        return sameTitleCount == 0 ? title : $"{title} #{sameTitleCount + 1}";
    }

    private static async Task<int> GetSelectionLengthAsync(EditorModel model)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return model.RuntimeSelection.Length;
        }

        return await Dispatcher.UIThread.InvokeAsync(() => model.RuntimeSelection.Length);
    }

    private static async Task<string> GetSelectedTextAsync(EditorModel model)
    {
        var textEditor = model.TextEditor;
        if (textEditor is null)
        {
            return string.Empty;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            return textEditor.GetSelectedText();
        }

        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var editor = model.TextEditor;
            return editor?.GetSelectedText() ?? string.Empty;
        });
    }

    private static async Task<int> GetLineCountAsync(EditorModel model)
    {
        string text = await GetTextAsync(model);
        // 统计换行符数量，空文本视为 1 行
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        int count = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                count++;
            }
        }

        return count;
    }

    private static async Task<string> GetTextAsync(EditorModel model)
    {
        var textEditor = model.TextEditor;
        if (textEditor is null)
        {
            return string.Empty;
        }

        // 切换到 UI 线程读取 TextEditor.Text
        if (Dispatcher.UIThread.CheckAccess())
        {
            return textEditor.Text;
        }

        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var editor = model.TextEditor;
            return editor?.Text ?? string.Empty;
        });
    }

    private static string FormatTabList(List<TabListInfo> tabInfos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("标签列表（当前标签用 [当前] 标记）:");

        foreach (TabListInfo info in tabInfos)
        {
            string currentMarker = info.IsCurrent ? "[当前] " : string.Empty;
            string selectionMarker = info.SelectionLength > 0 ? $" [选中 {info.SelectionLength} 个字符]" : string.Empty;
            string fileInfo = info.FilePath ?? "未保存";
            sb.AppendLine($"- {currentMarker}\"{info.Title}\" (#{info.TabId}){selectionMarker} | 文件: {fileInfo} | {info.LineCount} 行");
        }

        return sb.ToString();
    }

    private readonly record struct TabListInfo(string TabId, string Title, string? FilePath, int LineCount, bool IsCurrent, int SelectionLength);
}