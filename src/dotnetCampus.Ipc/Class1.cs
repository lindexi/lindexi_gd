using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Schema;

namespace dotnetCampus.Ipc
{
    public class IpcObjectJsonSerializer : IIpcObjectSerializer
    {
        public byte[] Serialize(object obj)
        {
            using var memoryStream = new MemoryStream();
            var utf8JsonWriter = new Utf8JsonWriter(memoryStream);
            JsonSerializer.Serialize(utf8JsonWriter, obj);
            return memoryStream.ToArray();
        }

        public T Deserialize<T>(byte[] byteList)
        {
            return JsonSerializer.Deserialize<T>(byteList);
        }
    }

    public interface IIpcObjectSerializer
    {
        byte[] Serialize(object obj);

        T Deserialize<T>(byte[] byteList);
    }
}
