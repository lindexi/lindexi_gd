using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ChatRoom.AvaloniaShell.Views;

/// <summary>
/// 角色大厅视图。
/// </summary>
public partial class RoleLobbyView : UserControl
{
    /// <summary>
    /// 初始化角色大厅视图。
    /// </summary>
    public RoleLobbyView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
