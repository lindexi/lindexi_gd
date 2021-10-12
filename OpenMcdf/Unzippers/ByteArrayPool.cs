namespace OpenMcdf
{
    public class ByteArrayPool : IByteArrayPool
    {
        public byte[] Rent(int minimumLength)
        {
            return new byte[minimumLength];
        }

        public void Return(byte[] byteList)
        {
        }

    }
}