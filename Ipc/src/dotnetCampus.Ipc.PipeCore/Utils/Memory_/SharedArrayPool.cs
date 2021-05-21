using System.Buffers;

namespace dotnetCampus.Ipc.PipeCore.Utils
{
    /// <summary>
    ///     共享数组内存，底层使用 ArrayPool 实现
    /// </summary>
    public class SharedArrayPool : ISharedArrayPool
    {
        /// <summary>
        ///     创建共享数组
        /// </summary>
        /// <param name="arrayPool"></param>
        public SharedArrayPool(ArrayPool<byte>? arrayPool = null)
        {
            ArrayPool = arrayPool ?? ArrayPool<byte>.Shared;
        }

        /// <summary>
        ///     使用的数组池
        /// </summary>
        public ArrayPool<byte> ArrayPool { get; }

        /// <inheritdoc />
        public byte[] Rent(int minLength)
        {
            return ArrayPool.Rent(minLength);
        }

        /// <inheritdoc />
        public void Return(byte[] array)
        {
            ArrayPool.Return(array);
        }
    }
}
