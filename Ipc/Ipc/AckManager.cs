using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ipc
{
    internal class AckManager
    {
        private ulong _currentAck;

        public AckManager(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        private IpcContext IpcContext { get; }

        public Ack CurrentAck
        {
            set
            {
                lock (Locker)
                {
                    var ack = value.Value;
                    if (ack > ulong.MaxValue - ushort.MaxValue) ack = 0;

                    _currentAck = ack;
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

        // ACK 0x41, 0x43, 0x4B
        public byte[] AckHeader { get; } = {0x41, 0x43, 0x4B};

        private object Locker => AckHeader;

        public Ack GetAck()
        {
            lock (Locker)
            {
                CurrentAck = CurrentAck.Value + 1;
                while (AckTaskList.TryGetValue(CurrentAck.Value, out _))
                {
                    CurrentAck = CurrentAck.Value + 1;
                }
            }

            return CurrentAck;
        }

        public bool IsAckMessage(Stream stream, out Ack ack)
        {
            var position = stream.Position;
            if (IsAckMessageInner(stream, out ack)) return true;

            stream.Position = position;

            return false;
        }

        public byte[] BuildAckMessage(Ack receivedAck)
        {
            const int ackLength = sizeof(ulong) + sizeof(ulong);
            byte[] buffer = new byte[AckHeader.Length + ackLength];

            Array.Copy(AckHeader, buffer, AckHeader.Length);

            var memoryStream = new MemoryStream(buffer)
            {
                Position = AckHeader.Length
            };
            var binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(receivedAck.Value);
            lock (Locker)
            {
                CurrentAck = Math.Max(CurrentAck.Value, receivedAck.Value);
            }

            CurrentAck = CurrentAck.Value + 1;
            binaryWriter.Write(CurrentAck.Value);

            return buffer;
        }

        private bool IsAckMessageInner(Stream stream, out Ack ack)
        {
            /*
             * AckHeader
             * 回复的 ACK ulong
             * 推荐的下一次使用的Ack ulong
             */
            ack = 0;
            const int ackLength = sizeof(ulong) + sizeof(ulong);
            if (stream.Length - stream.Position != AckHeader.Length + ackLength) return false;

            if (!IsAckHeader(stream)) return false;

            var binaryReader = new BinaryReader(stream);
            ack = binaryReader.ReadUInt64();
            var nextAck = binaryReader.ReadUInt64();

            lock (Locker)
            {
                CurrentAck = Math.Max(CurrentAck.Value, nextAck);
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

        internal async void RegisterAckTask(AckTask ackTask)
        {
            lock (Locker)
            {
                if (AckTaskList.TryAdd(ackTask.Ack.Value, ackTask))
                {
                }
                else
                {
                    // 理论上是找不到的
                }
            }

            await ackTask.Task.Task;

            lock (Locker)
            {
                if (AckTaskList.Remove(ackTask.Ack.Value, out var removedTask))
                {
                    Debug.Assert(ReferenceEquals(removedTask, ackTask));
                }
                else
                {
                    Debug.Assert(false, "收到的一定存在");
                }
            }
        }

        internal void OnAckReceived(object? sender, AckArgs e)
        {
            AckTask ackTask;

            lock (Locker)
            {
                if (!AckTaskList.TryGetValue(e.Ack.Value, out ackTask!))
                {
                    // 被干掉了，也许是因为等待太久
                    return;
                }
            }

            if (ackTask.ClientName.Equals(e.ClientName))
            {
                // 此时也许是等待太久
                ackTask.Task.TrySetResult(true);
            }
            else
            {
                // 不是发生给这个客户端的，只是 ack 相同，这个类被改错
            }
        }

        private Dictionary<ulong, AckTask> AckTaskList { get; } = new Dictionary<ulong, AckTask>();
    }
}