namespace OpenMcdf
{
    public class ByteArrayPool : IByteArrayPool
    {
        public byte[] Rent(int minimumLength)
        {
            if (_buffer != null)
            {
                if (_buffer.Length >= minimumLength)
                {
                    var buffer = _buffer;
                    _buffer = null;
                    return buffer;
                }
            }

            return new byte[minimumLength];
        }

        public void Return(byte[] byteList)
        {
            if (byteList == null || byteList.Length >= MaxBufferLength)
            {
                return;
            }

            if (_buffer is null || byteList.Length > _buffer.Length)
            {
                _buffer = byteList;
            }
        }

        private byte[] _buffer;
        private const int MaxBufferLength = 8 * 1024;
    }
}