using System;
using System.IO;

namespace Ipc
{
    class AckManager
    {
        private IpcContext IpcContext { get; }

        public AckManager(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        public ulong GetAck()
        {
            CurrentAck++;
            return CurrentAck;
        }

        private ulong _currentAck;

        public ulong CurrentAck
        {
            set
            {
                lock (Locker)
                {
                    if (value > ulong.MaxValue - ushort.MaxValue)
                    {
                        value = 0;
                    }

                    _currentAck = value;
                }
            }
            get
            {
                lock (Locker)
                {
                    return _currentAck;
                }
            }
        }

        public bool IsAckMessage(Stream stream, out ulong ack)
        {
            var position = stream.Position;
            if (IsAckMessageInner(stream, out ack))
            {
                return true;
            }

            stream.Position = position;

            return false;
        }

        public byte[] BuildAckMessage(ulong receivedAck)
        {
            const int ackLength = sizeof(ulong) + sizeof(ulong);
            byte[] buffer = new byte[AckHeader.Length + ackLength];

            Array.Copy(AckHeader, buffer, AckHeader.Length);

            var memoryStream = new MemoryStream(buffer)
            {
                Position = AckHeader.Length
            };
            var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(receivedAck);
            lock (Locker)
            {
                CurrentAck = Math.Max(CurrentAck, receivedAck);
            }
            CurrentAck++;
            binaryWriter.Write(CurrentAck);

            return buffer;
        }

        private bool IsAckMessageInner(Stream stream, out ulong ack)
        {
            /*
             * AckHeader
             * 回复的 ACK ulong
             * 推荐的下一次使用的Ack ulong
             */
            ack = 0;
            const int ackLength = sizeof(ulong) + sizeof(ulong);
            if (stream.Length - stream.Position != AckHeader.Length + ackLength)
            {
                return false;
            }

            if (!IsAckHeader(stream)) return false;

            var binaryReader = new BinaryReader(stream);
            ack = binaryReader.ReadUInt64();
            var nextAck = binaryReader.ReadUInt64();

            lock (Locker)
            {
                CurrentAck = Math.Max(CurrentAck, nextAck);
            }

            return false;
        }

        private bool IsAckHeader(Stream stream)
        {
            foreach (var ack in AckHeader)
            {
                if (stream.ReadByte() != ack)
                {
                    return false;
                }
            }

            return true;
        }

        // ACK 0x41, 0x43, 0x4B
        public byte[] AckHeader { get; } = new byte[] {0x41, 0x43, 0x4B};

        private object Locker => AckHeader;
    }
}