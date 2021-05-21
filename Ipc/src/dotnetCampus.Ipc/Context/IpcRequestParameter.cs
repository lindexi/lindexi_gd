using System;

namespace dotnetCampus.Ipc.Context
{
    public class IpcRequestParameter
    {
        public IpcSerializableType ParameterType { set; get; }

        public object Value { set; get; }
    }
}
