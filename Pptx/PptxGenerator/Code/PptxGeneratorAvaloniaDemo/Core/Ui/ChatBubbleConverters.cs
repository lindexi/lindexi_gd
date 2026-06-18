using AgentLib.Model;

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

using Microsoft.Extensions.AI;

using System;
using System.Globalization;

namespace PptxGenerator;

/// <summary>
/// 聊天气泡水平对齐转换器。
/// 用户消息右对齐，助手/系统消息左对齐。
/// </summary>
public sealed class ChatBubbleAlignmentConverter : IValueConverter
{
    public static readonly ChatBubbleAlignmentConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role)
        {
            return role == ChatRole.User ? Avalonia.Layout.HorizontalAlignment.Right : Avalonia.Layout.HorizontalAlignment.Left;
        }

        return Avalonia.Layout.HorizontalAlignment.Left;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 聊天气泡背景颜色转换器。
/// 用户消息蓝色背景，其他浅灰背景。
/// </summary>
public sealed class ChatBubbleBackgroundConverter : IValueConverter
{
    public static readonly ChatBubbleBackgroundConverter Instance = new();

    private static readonly IBrush UserBrush = Brush.Parse("#FFEFF6FF");
    private static readonly IBrush AssistantBrush = Brush.Parse("#FFF8FAFC");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role)
        {
            return role == ChatRole.User ? UserBrush : AssistantBrush;
        }

        return AssistantBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 聊天气泡作者名颜色转换器。
/// </summary>
public sealed class ChatBubbleAuthorColorConverter : IValueConverter
{
    public static readonly ChatBubbleAuthorColorConverter Instance = new();

    private static readonly IBrush UserBrush = Brush.Parse("#FF2563EB");
    private static readonly IBrush AssistantBrush = Brush.Parse("#FF64748B");
    private static readonly IBrush SystemBrush = Brush.Parse("#FF94A3B8");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ChatRole role)
        {
            return role == ChatRole.User ? UserBrush
                : role == ChatRole.System ? SystemBrush
                : AssistantBrush;
        }

        return AssistantBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}