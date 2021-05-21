using System;

namespace dotnetCampus.Ipc.Context
{
    public class IpcRequestParameterType : IpcSerializableType
    {
        public IpcRequestParameterType()
        {
        }

        public IpcRequestParameterType(Type type) : base(type)
        {
        }
    }
}
