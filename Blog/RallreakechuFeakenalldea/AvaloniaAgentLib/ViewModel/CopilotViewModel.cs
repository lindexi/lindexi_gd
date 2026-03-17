using AvaloniaAgentLib.Model;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.ViewModel;

public class CopilotViewModel : INotifyPropertyChanged
{
    private bool _isChatting;

    public CopilotViewModel()
    {
        AddAssistantWelcomeMessage();
    }

    public ObservableCollection<CopilotChatMessage> ChatMessages { get; } = [];

    public bool IsChatting
    {
        get => _isChatting;
        private set
        {
            if (!SetField(ref _isChatting, value))
            {
                return;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditInput));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    /// <summary>
    /// 能否编辑输入
    /// </summary>
    public bool CanEditInput => !IsChatting;

    public string SendButtonText => IsChatting ? "停止" : "发送";

    public void Clear()
    {
        ChatMessages.Clear();
        AddAssistantWelcomeMessage();
    }

    public async Task SendMessageAsync(string? inputText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        IsChatting = true;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            ChatMessages.Add(CopilotChatMessage.CreateUser(inputText));
            await Task.Delay(TimeSpan.FromMilliseconds(4000), cancellationToken);
            ChatMessages.Add(CopilotChatMessage.CreateAssistant("消息已接收。待接入 Agent 后将返回真实回复。"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ChatMessages.Add(CopilotChatMessage.CreateAssistant("已取消"));
        }
        finally
        {
            IsChatting = false;
        }
    }

    private void AddAssistantWelcomeMessage()
    {
        ChatMessages.Add(CopilotChatMessage.CreateAssistant("你好，我是 Copilot。请开始输入你的问题。"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
