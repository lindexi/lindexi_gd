using System;

namespace dotnetCampus.Ipc.Context
{
    public class IpcRequestParameter
    {
        public Type ParameterType { set; get; }

        public object Value { set; get; }
    }
}
