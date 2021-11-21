using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Utils;

namespace dotnetCampus.Ipc
{
    public class IpcGetObjectRequest
    {
        public IpcSerializableType ObjectType { set; get; }
    }

    public class IpcRequestManager
    {
        public IPeerProxy PeerProxy { set; get; } = null!;

        public IIpcObjectSerializer IpcObjectSerializer { set; get; } = new IpcObjectJsonSerializer();

        public Task<TResponse> GetResponseAsync<TRequest, TResponse>(TRequest request)
        {
            throw new NotImplementedException();
        }
    }



    /// <summary>
    /// 提供客户端使用的方法，可以拿到服务器端的对象
    /// </summary>
    /// 在服务器端将会维护一个对象池，可以选的是一个对象可以作为单例或者作为每次访问返回新的对象
    /// 如果是每次访问都返回的新的对象，就需要将这个对象发生过去的时候加上对象的标识，之后客户端调用的时候，就可以
    /// 使用这个标识拿到对应的对象
    public class IpcClientProvider
    {
        /// <summary>
        /// 从远程获取到对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Task<T> GetObjectAsync<T>()
        {
            var type = typeof(T);
            var ipcSerializableType = new IpcSerializableType(type);
            var ipcGetObjectRequest = new IpcGetObjectRequest()
            {
                ObjectType = ipcSerializableType
            };

            throw new NotImplementedException();
        }

        public IpcRequestManager IpcRequestManager { set; get; } = new IpcRequestManager();



        public IPeerProxy PeerProxy { set; get; } = null!;

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
