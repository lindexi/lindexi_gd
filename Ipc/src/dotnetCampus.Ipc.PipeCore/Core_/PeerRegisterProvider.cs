using System;
using System.IO;
using System.Text;
using dotnetCampus.Ipc.Abstractions.Context;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    /// <summary>
    /// 用于进行 Ipc 连接时的建立通讯，建立通讯的时候需要向对方发送自己的管道名，用于让对方连接
    /// </summary>
    internal class PeerRegisterProvider
    {
        public IpcBufferMessageContext BuildPeerRegisterMessage(string peerName)
        {
            /*
             * byte[] Header
             * Int32 PipeNameLength
             * byte[] PipeName
             */
            var peerRegisterHeaderIpcBufferMessage = new IpcBufferMessage(PeerRegisterHeader);
            var buffer = Encoding.UTF8.GetBytes(peerName);

            var peerNameLengthBufferMessage = new IpcBufferMessage(BitConverter.GetBytes(buffer.Length));
            var peerNameIpcBufferMessage = new IpcBufferMessage(buffer);

            return new IpcBufferMessageContext($"PeerRegisterMessage PipeName={peerName}",
                IpcMessageCommandType.PeerRegister, peerRegisterHeaderIpcBufferMessage, peerNameLengthBufferMessage,
                peerNameIpcBufferMessage);
        }

        public bool TryParsePeerRegisterMessage(Stream stream, out string peerName)
        {
            var position = stream.Position;
            var isPeerRegisterMessage = TryParsePeerRegisterMessageInner(stream, position, out peerName);
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

        private bool TryParsePeerRegisterMessageInner(Stream stream, long position, out string peerName)
        {
            peerName = string.Empty;
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

            var binaryReader = new BinaryReader(stream);
            var peerNameLength = binaryReader.ReadInt32();
            peerName = new string(binaryReader.ReadChars(peerNameLength));
            return true;
        }

        // PeerRegisterHeader 0x50, 0x65, 0x65, 0x72, 0x52, 0x65, 0x67, 0x69, 0x73, 0x74, 0x65, 0x72, 0x48, 0x65, 0x61, 0x64, 0x65, 0x72, 0x20
        private byte[] PeerRegisterHeader { get; } =
        {
            0x50, 0x65, 0x65, 0x72, 0x52, 0x65, 0x67, 0x69, 0x73, 0x74, 0x65, 0x72, 0x48, 0x65, 0x61, 0x64, 0x65, 0x72,
            0x20
        };
    }
}
