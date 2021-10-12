using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace OpenMcdf
{
    /// <summary>
    /// Unzip
    /// </summary>
    public static class CompoundFileUnzipper
    {
        public static void Unzip(string sourceCompoundFileName, string destinationDirectoryName)
        {

        }

        public static void Unzip(Stream sourceCompoundFileStream, string destinationDirectoryName, IByteArrayPool byteArrayPool = null)
        {
            byteArrayPool ??= new ByteArrayPool();

            Stream stream;
            if (sourceCompoundFileStream.CanSeek is false)
            {
                stream = new ForwardSeekStream(sourceCompoundFileStream, byteArrayPool);
            }
            else
            {
                stream = sourceCompoundFileStream;
            }

            var compoundFile = new CompoundFile(stream, CFSUpdateMode.ReadOnly, CFSConfiguration.Default);


        }
    }
}
