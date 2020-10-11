using System.IO;
using System.Text;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 用于进行 Ipc 连接时的建立通讯，建立通讯的时候需要向对方发送自己的管道名，用于让对方连接
    /// </summary>
    class PeerRegisterProvider
    {
        public IpcBufferMessageContext BuildPeerRegisterMessage(string pipeName)
        {
            var peerRegisterHeaderIpcBufferMessage = new IpcBufferMessage(PeerRegisterHeader);
            var buffer = Encoding.UTF8.GetBytes(pipeName);
            var pipeNameIpcBufferMessage = new IpcBufferMessage(buffer);

            return new IpcBufferMessageContext($"PeerRegisterMessage PipeName={pipeName}", peerRegisterHeaderIpcBufferMessage, pipeNameIpcBufferMessage);
        }

        public bool TryParsePeerRegisterMessage(Stream stream, out string pipeName)
        {
            var position = stream.Position;
            var isPeerRegisterMessage = TryParsePeerRegisterMessageInner(stream, position, out pipeName);
            if (isPeerRegisterMessage)
            {
                return true;
            }
            else
            {
                stream.Position = position;
                return false;
            }
        }

        private bool TryParsePeerRegisterMessageInner(Stream stream, long position, out string pipeName)
        {
            pipeName = string.Empty;
            if (stream.Length - position <= PeerRegisterHeader.Length)
            {
                return false;
            }

            for (var i = 0; i < PeerRegisterHeader.Length; i++)
            {
                if (stream.ReadByte() != PeerRegisterHeader[i])
                {
                    return false;
                }
            }

            var streamReader = new StreamReader(stream, Encoding.UTF8);
            pipeName = streamReader.ReadToEnd();
            return true;
        }

        // PeerRegisterHeader 0x50, 0x65, 0x65, 0x72, 0x52, 0x65, 0x67, 0x69, 0x73, 0x74, 0x65, 0x72, 0x48, 0x65, 0x61, 0x64, 0x65, 0x72, 0x20
        private byte[] PeerRegisterHeader { get; } = { 0x50, 0x65, 0x65, 0x72, 0x52, 0x65, 0x67, 0x69, 0x73, 0x74, 0x65, 0x72, 0x48, 0x65, 0x61, 0x64, 0x65, 0x72, 0x20 };
    }
}