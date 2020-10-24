namespace dotnetCampus.Ipc
{
    public interface IIpcObjectSerializer
    {
        byte[] Serialize(object obj);

        T Deserialize<T>(byte[] byteList);
    }
}