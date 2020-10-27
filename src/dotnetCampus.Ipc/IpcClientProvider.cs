using System;
using System.Diagnostics;
using System.Reflection;

namespace dotnetCampus.Ipc
{
    /// <summary>
    /// 提供客户端使用的方法，可以拿到服务器端的对象
    /// </summary>
    /// 在服务器端将会维护一个对象池，可以选的是一个对象可以作为单例或者作为每次访问返回新的对象
    /// 如果是每次访问都返回的新的对象，就需要将这个对象发生过去的时候加上对象的标识，之后客户端调用的时候，就可以
    /// 使用这个标识拿到对应的对象
    public class IpcClientProvider
    {
        public T GetObject<T>()
        {
#if NETCOREAPP
            var obj = DispatchProxy.Create<T, IpcProxy<T>>();
            var ipcProxy = obj as IpcProxy<T>;
            Debug.Assert(ipcProxy != null);
            ipcProxy!.IpcClientProvider = this;
            return obj;
#endif
            // 还需要加上 NET45 使用的透明代理
            throw new NotImplementedException();
        }
    }
}
