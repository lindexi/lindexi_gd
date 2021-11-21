namespace dotnetCampus.Ipc.Abstractions
{
    /// <summary>
    /// 对象序列化器
    /// </summary>
    public interface IIpcObjectSerializer
    {
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// 反序列化对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="byteList"></param>
        /// <returns></returns>
        T Deserialize<T>(byte[] byteList);
    }
}
