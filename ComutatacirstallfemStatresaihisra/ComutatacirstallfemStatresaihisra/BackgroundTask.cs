using System;

namespace ComutatacirstallfemStatresaihisra
{
    /// <summary>
    /// 后台代码
    /// </summary>
    public class BackgroundTask
    {
        /// <inheritdoc />
        public BackgroundTask(string name, Action action, TimeSpan delayTime)
        {
            Name = name;
            Action = action;
            DelayTime = delayTime;
            LastStartTime = DateTime.Now;
        }

        public string Name { get; }

        public Action Action { get; }

        public TimeSpan DelayTime { get; }

        public DateTime LastStartTime { set; get; }
    }
}