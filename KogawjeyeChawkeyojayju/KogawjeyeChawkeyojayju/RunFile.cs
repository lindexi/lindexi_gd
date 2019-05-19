namespace KogawjeyeChawkeyojayju
{
    public class RunFile
    {
        /// <summary>
        /// 开启的程序名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 启动的文件
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Argument { set; get; }

        /// <summary>
        /// 如果写了进程名，那么这个进程只会启动一次
        /// </summary>
        public string ProcessName { set; get; }
    }
}