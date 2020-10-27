using System.Diagnostics;
using System.Reflection;

namespace dotnetCampus.Ipc
{
    public class IpcClientProvider
    {
        public T GetObject<T>()
        {
            var obj= DispatchProxy.Create<T, IpcProxy<T>>();
            var ipcProxy = obj as IpcProxy<T>;
            Debug.Assert(ipcProxy!=null);
            ipcProxy!.IpcClientProvider = this;
            return obj;
        }
    }
}
