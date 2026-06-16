using Avalonia.Controls;

namespace ChatRoomAvaloniaDemo.Views;

/// <summary>
/// 聊天室主视图。三列布局的用户控件，包含历史会话列、聊天消息列和角色列表列。
/// </summary>
public partial class ChatRoomMainView : UserControl
{
    public ChatRoomMainView()
    {
        InitializeComponent();
    }
}