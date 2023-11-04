using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FarearrecuyalFukairjebuce;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = ChatGptViewModel;
        InitializeComponent();
    }

    public ChatGptViewModel ChatGptViewModel { get; } = new ChatGptViewModel();
}

public class ChatGptViewModel
{
    public ChatGptViewModel()
    {
        ChatInfoList.Add(new ChatInfo(ChatItemType.User, "用户的内容，文本文本文本文本文本文本文本文本文本文本文本文本文本文本文本文本文本"));
        ChatInfoList.Add(new ChatInfo(ChatItemType.Assistant, "文本文本文本文本文本文本文本文本文本文本文本文本文本文本文本文本文本"));
        ChatInfoList.Add(new ChatInfo(ChatItemType.User, "文本文本文本"));
    }

    public ObservableCollection<ChatInfo> ChatInfoList { get; } = new ObservableCollection<ChatInfo>();

    public async Task ChatAsync(string input)
    {

    }
}

public class ChatListItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserChatItemDataTemplate { get; set; }
    public DataTemplate? AssistantChatItemDataTemplate { get; set; }


    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is ChatInfo chatInfo)
        {
            if (chatInfo.ChatItemType == ChatItemType.Assistant)
            {
                return AssistantChatItemDataTemplate;
            }
            else
            {
                return UserChatItemDataTemplate;
            }
        }

        return base.SelectTemplate(item, container);
    }
}

public enum ChatItemType
{
    User,
    Assistant,
}

public record ChatInfo(ChatItemType ChatItemType, string Text)
{
}