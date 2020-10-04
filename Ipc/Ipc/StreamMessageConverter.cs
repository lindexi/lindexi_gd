namespace Ipc
{
    //sealed class StreamMessageConverter
    //{
    //    public StreamMessageConverter(Stream stream, byte[] messageHeader, ISharedArrayPool sharedArrayPool)
    //    {
    //        Stream = stream;
    //        MessageHeader = messageHeader;
    //        SharedArrayPool = sharedArrayPool;

    //        var streamReader = new StreamReader(Stream);
    //        var binaryReader = new BinaryReader(Stream);

    //        StreamReader = streamReader;
    //        BinaryReader = binaryReader;
    //    }

    //    public async void Start()
    //    {
    //        while (true)
    //        {
    //            try
    //            {
    //                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(Stream, MessageHeader, SharedArrayPool);
    //                if (success)
    //                {
    //                    OnMessageReceived(new ByteListMessageStream(ipcMessageContext.MessageBuffer, (int) ipcMessageContext.MessageLength, SharedArrayPool));
    //                }
    //                else
    //                {

    //                }

    //                //await GetHeader();

    //                //// 读取长度
    //                //// 后续优化，使用异步读取，可以使用 stackalloc 减少内存使用
    //                //var messageLength = BinaryReader.ReadUInt32();

    //                //if (messageLength > ushort.MaxValue * 1024)
    //                //{
    //                //    // 太长了
    //                //    continue;
    //                //}

    //                //var messageBuffer = SharedArrayPool.Rent((int)messageLength);

    //                //try
    //                //{
    //                //    var readCount = await Stream.ReadAsync(messageBuffer, 0, (int)messageLength).ConfigureAwait(false);

    //                //    Debug.Assert(readCount == messageLength);
    //                //    OnMessageReceived(new ByteListMessageStream(messageBuffer, readCount, SharedArrayPool));
    //                //}
    //                //finally
    //                //{
    //                //    // 为什么不适合使用 SharedArrayPool 方法？原因是将会作为事件给出去使用，此时如果业务层存放这个数组，在使用的时候被更改就不好玩了
    //                //    //SharedArrayPool.Return(messageBuffer);
    //                //}
    //            }
    //            catch (Exception e)
    //            {
    //                Console.WriteLine(e);
    //            }
    //        }
    //    }

    //    public event EventHandler<ByteListMessageStream>? MessageReceived;

    //    //private static async void ReadByteListAsync(Stream stream, ISharedArrayPool sharedArrayPool, int count)
    //    //{
    //    //    var messageBuffer = sharedArrayPool.Rent(count);

    //    //    try
    //    //    {
    //    //        var readCount = await stream.ReadAsync(messageBuffer, 0, count).ConfigureAwait(false);

    //    //    }
    //    //    finally
    //    //    {
    //    //        sharedArrayPool.Return(messageBuffer);
    //    //    }
    //    //}

    //    private Stream Stream { get; }

    //    public byte[] MessageHeader { get; }

    //    public ISharedArrayPool SharedArrayPool { get; }
    //    public StreamReader StreamReader { get; }
    //    public BinaryReader BinaryReader { get; }

    //    private void OnMessageReceived(ByteListMessageStream byteListMessageStream)
    //    {
    //        MessageReceived?.Invoke(this, byteListMessageStream);
    //    }
    //}
}