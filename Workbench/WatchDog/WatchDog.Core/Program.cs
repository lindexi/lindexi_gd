using Timer = System.Timers.Timer;

namespace WatchDog.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }

    public class WatchDogProvider
    {
        public void Feed()
        {

        }

        public void GetWang()
        {

        }

        public void Mute()
        {

        }
    }

    /// <summary>
    /// 喂狗信息
    /// </summary>
    /// <param name="Name">名称</param>
    /// <param name="Status">状态</param>
    /// <param name="Id">Id号，如为空则自动分配</param>
    /// <param name="DelaySecond">喂狗允许的延迟时间，超过时间就被狗咬</param>
    /// <param name="MaxDelayCount">最多的次数，一般是 1 的值</param>
    /// <param name="NotifyIntervalSecond">通知的间隔时间</param>
    /// <param name="NotifyMaxCount">最多的通知次数，默认是无限通知</param>
    public record FeedDogInfo
    (
        string Name,
        string Status,
        string? Id = null,
        uint DelaySecond = 60,
        uint MaxDelayCount = 1,
        uint NotifyIntervalSecond = 60 * 30,
        int NotifyMaxCount = -1
    );
    /// <summary>
    /// 喂狗的结果，返回的是喂狗的信息
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="DelaySecond"></param>
    /// <param name="MaxDelayCount"></param>
    /// <param name="NotifyIntervalSecond"></param>
    /// <param name="NotifyMaxCount"></param>
    /// <param name="RegisterTime">注册时间</param>
    /// <param name="IsRegister">这一条是否输出注册的，首次喂狗</param>
    public record FeedDogResult(string Id, uint DelaySecond, uint MaxDelayCount, uint NotifyIntervalSecond, int NotifyMaxCount, DateTimeOffset RegisterTime,bool IsRegister);

    /// <summary>
    /// 获取 Wang 咬人信息
    /// </summary>
    /// <param name="DogId"></param>
    public record GetWangInfo(string DogId);

    /// <summary>
    /// 获取 Wang 咬人的结果
    /// </summary>
    /// <param name="DogId"></param>
    public record GetWangResult(string DogId);

    public record WangInfo(string Id, string Name, string LastStatus, uint DelaySecond, DateTimeOffset LastUpdateTime, DateTimeOffset RegisterTime);

    /// <summary>
    /// 禁言信息
    /// </summary>
    /// <param name="Id">禁掉哪一条</param>
    /// <param name="DogId">属于哪只狗的</param>
    /// <param name="ShouldRemove">是否应该移除掉记录。如果设置为 true 则无视 <paramref name="SilentSecond"/> 等选项，对所有狗生效，此属性为 true 时，无视后续其他属性</param>
    /// <param name="SilentSecond">静默的时间，默认静默一个小时</param>
    /// <param name="ActiveInNextFeed">自动下次喂狗时激活，默认是 true 用于服务自动恢复。为 false 则永久移除，无视服务状态</param>
    /// <param name="MuteAll">对这一条禁用所有的狗，默认 false 只对当前狗生效</param>
    public record MuteInfo
    (
        string Id,
        string DogId,
        bool ShouldRemove = false,
        uint SilentSecond = 3600,
        bool ActiveInNextFeed = true,
        bool MuteAll = false
    );
}
