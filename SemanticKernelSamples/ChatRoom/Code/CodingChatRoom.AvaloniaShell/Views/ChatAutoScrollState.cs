using System;

namespace CodingChatRoom.AvaloniaShell.Views;

/// <summary>
/// 维护消息列表是否应继续跟随底部。
/// </summary>
internal sealed class ChatAutoScrollState
{
    private const double BottomThreshold = 48;
    private const double DeltaTolerance = 0.01;

    /// <summary>
    /// 获取当前是否应跟随消息尾部。
    /// </summary>
    public bool ShouldFollowTail { get; private set; } = true;

    /// <summary>
    /// 获取是否已有等待 UI 线程执行的滚动请求。
    /// </summary>
    public bool IsScrollPending { get; private set; }

    /// <summary>
    /// 重置为跟随消息尾部。
    /// </summary>
    public void Reset() => ShouldFollowTail = true;

    /// <summary>
    /// 尝试登记一个待执行的滚动请求，避免流式布局变化持续堆积 UI 调度任务。
    /// </summary>
    public bool TryScheduleScroll()
    {
        if (IsScrollPending)
        {
            return false;
        }

        IsScrollPending = true;
        return true;
    }

    /// <summary>
    /// 标记已登记的滚动请求执行完成。
    /// </summary>
    public void CompleteScheduledScroll() => IsScrollPending = false;

    /// <summary>
    /// 处理一次滚动状态变化。
    /// </summary>
    /// <returns>当前变化是否需要主动滚动到底部。</returns>
    public bool HandleScrollChanged(
        double offsetY,
        double extentHeight,
        double viewportHeight,
        double extentDeltaY,
        double viewportDeltaY,
        double offsetDeltaY)
    {
        bool layoutChanged = Math.Abs(extentDeltaY) > DeltaTolerance
            || Math.Abs(viewportDeltaY) > DeltaTolerance;

        if (!layoutChanged && Math.Abs(offsetDeltaY) > DeltaTolerance)
        {
            ShouldFollowTail = IsNearBottom(offsetY, extentHeight, viewportHeight);
        }

        return layoutChanged && ShouldFollowTail;
    }

    private static bool IsNearBottom(double offsetY, double extentHeight, double viewportHeight)
        => extentHeight - viewportHeight - offsetY <= BottomThreshold;
}
