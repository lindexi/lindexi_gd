namespace dotnetCampus.Ipc.PipeCore.Utils
{
    /// <summary>
    ///     提供共享的数组
    /// </summary>
    // 用于后续支持 .NET 4.5 版本，此版本没有 ArrayPool 类
    public interface ISharedArrayPool
    {
        /// <summary>
        ///     租借数组，要求返回的数组长度最小是 <paramref name="minLength" /> 长度
        /// </summary>
        /// <param name="minLength"></param>
        /// <returns></returns>
        byte[] Rent(int minLength);

        /// <summary>
        ///     将租借的数组返回，将会被下次租借使用
        /// </summary>
        /// <param name="array"></param>
        void Return(byte[] array);
    }
}
