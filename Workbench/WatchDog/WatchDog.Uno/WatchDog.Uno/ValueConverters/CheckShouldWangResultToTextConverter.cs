using WatchDog.Core.Context;

namespace WatchDog.Uno.ValueConverters;

static class CheckShouldWangResultToTextConverter
{
    public static string ToText(CheckShouldWangResult result)
    {
        if (result.ShouldWang)
        {
            return "汪";
        }

        if (result.ShouldMute)
        {
            return "汪(静默)";
        }

        if (result.OverNotifyMaxCount)
        {
            return "汪(超过最大通知次数)";
        }

        if (result.InNotifyInterval)
        {
            return "汪";
        }

        return "OK";
    }
}
