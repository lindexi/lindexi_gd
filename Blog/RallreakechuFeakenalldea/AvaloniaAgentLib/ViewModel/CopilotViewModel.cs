using AvaloniaAgentLib.Model;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.ViewModel;

public class CopilotViewModel : INotifyPropertyChanged
{
    public CopilotViewModel()
    {
        AddAssistantWelcomeMessage();
    }

    public ObservableCollection<CopilotChatMessage> ChatMessages { get; } = [];

    public void Clear()
    {
        ChatMessages.Clear();
        AddAssistantWelcomeMessage();
    }

    public async Task SendMessageAsync(string? inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        ChatMessages.Add(CopilotChatMessage.CreateUser(inputText));
        ChatMessages.Add(CopilotChatMessage.CreateAssistant("消息已接收。待接入 Agent 后将返回真实回复。"));
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
