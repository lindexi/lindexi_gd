using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    // Provides methods for processing file system strings in a cross-platform manner.
    // Most of the methods don't do a complete parsing (such as examining a UNC hostname),
    // but they will handle most string operations.
    public static partial class Path
    {
        // For generating random file names
        // 8 random bytes provides 12 chars in our encoding for the 8.3 name.
        private const int KeyLength = 8;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete(
            "Path.InvalidPathChars has been deprecated. Use GetInvalidPathChars or GetInvalidFileNameChars instead.")]
        public static readonly char[] InvalidPathChars = GetInvalidPathChars();

        private static char[] GetInvalidPathChars()
        {
            throw null;
        }
    }
}