using System.Collections.Generic;

namespace 分型
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    /// <summary>
    /// 点可以是超点，也可以是点
    /// </summary>
    /// 网络其实就是一个点
    class 原子点
    {
        public void 执行()
        {

        }

        public List<信息> 输入 { get; } = new();
        public List<信息> 输出 { get; } = new();
    }

    // 如何进行划分

    class 信息
    {
        /// <summary>
        /// 表示某些输入的信息是相同的一条信息
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// 表示这是第几条信息
        /// </summary>
        /// 信息将会多次进行输入，这个属性表示这是第几次输入的信息
        public uint Number { get; set; }
    }
}
