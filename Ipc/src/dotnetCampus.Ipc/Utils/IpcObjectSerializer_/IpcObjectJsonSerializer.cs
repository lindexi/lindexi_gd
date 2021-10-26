using System.Text;
using dotnetCampus.Ipc.Abstractions;
using Newtonsoft.Json;

namespace dotnetCampus.Ipc.Utils
{
    public class IpcObjectJsonSerializer : IIpcObjectSerializer
    {
        public byte[] Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] byteList)
        {
            var json = Encoding.UTF8.GetString(byteList);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
