using System.Text;
using Newtonsoft.Json;

namespace dotnetCampus.Ipc
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
