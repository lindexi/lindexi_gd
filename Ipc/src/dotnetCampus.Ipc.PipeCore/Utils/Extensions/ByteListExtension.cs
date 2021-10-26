namespace dotnetCampus.Ipc.PipeCore.Utils.Extensions
{
    internal static class ByteListExtension
    {
        public static bool Equals(byte[] a, byte[] b, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Equals(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;

            if (a.Length != b.Length) return false;

            return Equals(a, b, a.Length);
        }
    }
}
