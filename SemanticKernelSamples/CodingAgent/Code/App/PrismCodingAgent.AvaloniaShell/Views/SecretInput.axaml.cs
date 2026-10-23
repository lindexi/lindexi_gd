using System;
using Avalonia.Controls;

namespace PrismCodingAgent.AvaloniaShell.Views;

/// <summary>将密钥编辑与显隐切换合并为单个输入控件。</summary>
public partial class SecretInput : TextBox
{
    /// <summary>初始化默认掩码的密钥输入框。</summary>
    public SecretInput()
    {
        InitializeComponent();
    }

    protected override Type StyleKeyOverride => typeof(TextBox);
}
