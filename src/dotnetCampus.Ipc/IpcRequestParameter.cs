using System;

namespace dotnetCampus.Ipc
{
    public class IpcRequestParameter
    {
        public Type ParameterType { set; get; }

        public object Value { set; get; }
    }
}
