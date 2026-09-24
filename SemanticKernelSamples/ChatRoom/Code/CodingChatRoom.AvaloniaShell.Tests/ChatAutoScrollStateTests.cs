using CodingChatRoom.AvaloniaShell.Views;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ChatAutoScrollStateTests
{
    [TestMethod(DisplayName = "位于底部时内容增长应继续跟随")]
    [Timeout(5000)]
    public void ContentGrowthAtBottomShouldKeepFollowing()
    {
        var state = new ChatAutoScrollState();

        bool shouldScroll = state.HandleScrollChanged(
            offsetY: 600,
            extentHeight: 1000,
            viewportHeight: 400,
            extentDeltaY: 80,
            viewportDeltaY: 0,
            offsetDeltaY: 0);

        Assert.IsTrue(state.ShouldFollowTail);
        Assert.IsTrue(shouldScroll);
    }

    [TestMethod(DisplayName = "用户向上浏览后内容增长不应强制拉回底部")]
    [Timeout(5000)]
    public void UserScrollingUpShouldPauseFollowing()
    {
        var state = new ChatAutoScrollState();

        state.HandleScrollChanged(
            offsetY: 300,
            extentHeight: 1000,
            viewportHeight: 400,
            extentDeltaY: 0,
            viewportDeltaY: 0,
            offsetDeltaY: -300);
        bool shouldScroll = state.HandleScrollChanged(
            offsetY: 300,
            extentHeight: 1080,
            viewportHeight: 400,
            extentDeltaY: 80,
            viewportDeltaY: 0,
            offsetDeltaY: 0);

        Assert.IsFalse(state.ShouldFollowTail);
        Assert.IsFalse(shouldScroll);
    }

    [TestMethod(DisplayName = "用户重新滚回底部后应恢复跟随")]
    [Timeout(5000)]
    public void ReturningNearBottomShouldResumeFollowing()
    {
        var state = new ChatAutoScrollState();
        state.HandleScrollChanged(300, 1000, 400, 0, 0, -300);

        state.HandleScrollChanged(
            offsetY: 565,
            extentHeight: 1000,
            viewportHeight: 400,
            extentDeltaY: 0,
            viewportDeltaY: 0,
            offsetDeltaY: 265);
        bool shouldScroll = state.HandleScrollChanged(
            offsetY: 565,
            extentHeight: 1080,
            viewportHeight: 400,
            extentDeltaY: 80,
            viewportDeltaY: 0,
            offsetDeltaY: 0);

        Assert.IsTrue(state.ShouldFollowTail);
        Assert.IsTrue(shouldScroll);
    }
}
