using System;

namespace dotnetCampus.Ipc.Context
{
    /// <summary>
    /// 可以被进行序列化的 <see cref="Type"/> 类
    /// </summary>
    public class IpcSerializableType
    {
        public string? TypeFullName { get; set; }

        public IpcSerializableType()
        {
        }

        public IpcSerializableType(Type type)
        {
            TypeFullName = type.FullName;
        }
    }
}
