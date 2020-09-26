using System.Collections.Generic;

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 提供双缓存 线程安全列表
    /// </summary>
    /// 写入的时候写入到一个列表，通过 SwitchBuffer 方法，可以切换当前缓存
    public class DoubleBuffer<T> : DoubleBuffer<List<T>, T>
    {
        /// <summary>
        /// 创建使用 <see cref="List&lt;T&gt;"/> 的双缓存
        /// </summary>
        public DoubleBuffer() : base(new List<T>(), new List<T>())
        {
        }
    }
}
