using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static Kernel32;

const uint GENERIC_READ = 0x80000000;
const uint GENERIC_WRITE = 0x40000000;
const uint FILE_FLAG_OVERLAPPED = 0x40000000;

var fileHandle = new SafeFileHandle(Kernel32.CreateFile(@"\\?\hid#vid_1ff7&pid_0001&mi_00&col02#8&20f797d5&0&0001#{4d1e55b2-f16f-11cf-88cb-001111000030}", GENERIC_READ | GENERIC_WRITE,
    FileShareMode.FILE_SHARE_READ | FileShareMode.FILE_SHARE_WRITE, IntPtr.Zero, FileCreationDisposition.OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero), true);
var fileStream = new FileStream(fileHandle, FileAccess.ReadWrite, 4096, true);

var data = new byte[64];

await fileStream.WriteAsync(data, 0, data.Length);
// fileStream.Flush(flushToDisk: false);
await fileStream.FlushAsync(); // Fail, can not use Kernel32.FlushFileBuffers

class Kernel32
{
    public const CharSet BuildCharSet = CharSet.Unicode;
    public const string LibraryName = "kernel32";

    [DllImport(LibraryName, CharSet = BuildCharSet)]
    public static extern IntPtr CreateFile(string lpFileName,
        uint dwDesiredAccess,
        FileShareMode dwShareMode,
        IntPtr lpSecurityAttributes,
        FileCreationDisposition dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    public enum FileCreationDisposition
    {
        /// <summary>
        ///     Creates a new file, always.
        ///     If the specified file exists and is writable, the function overwrites the file, the function succeeds, and
        ///     last-error code is set to ERROR_ALREADY_EXISTS (183).
        ///     If the specified file does not exist and is a valid path, a new file is created, the function succeeds, and
        ///     the last-error code is set to zero.
        ///     For more information, see the Remarks section of this topic.
        /// </summary>
        CREATE_ALWAYS = 2,

        /// <summary>
        ///     Creates a new file, only if it does not already exist.
        ///     If the specified file exists, the function fails and the last-error code is set to
        ///     ERROR_FILE_EXISTS (80).
        ///     If the specified file does not exist and is a valid path to a writable location, a new file is created.
        /// </summary>
        CREATE_NEW = 1,

        /// <summary>
        ///     Opens a file, always.
        ///     If the specified file exists, the function succeeds and the last-error code is set to
        ///     ERROR_ALREADY_EXISTS (183).
        ///     If the specified file does not exist and is a valid path to a writable location, the function creates a
        ///     file and the last-error code is set to zero.
        /// </summary>
        OPEN_ALWAYS = 4,

        /// <summary>
        ///     Opens a file or device, only if it exists.
        ///     If the specified file or device does not exist, the function fails and the last-error code is set to
        ///     ERROR_FILE_NOT_FOUND (2).
        ///     For more information about devices, see the Remarks section.
        /// </summary>
        OPEN_EXISTING = 3,

        /// <summary>
        ///     Opens a file and truncates it so that its size is zero bytes, only if it exists.
        ///     If the specified file does not exist, the function fails and the last-error code is set to
        ///     ERROR_FILE_NOT_FOUND (2).
        ///     The calling process must open the file with the GENERIC_WRITE bit set as part of
        ///     the dwDesiredAccess parameter.
        /// </summary>
        TRUNCATE_EXISTING = 5
    }

    [Flags]
    public enum FileShareMode
    {
        /// <summary>
        ///     Prevents other processes from opening a file or device if they request delete, read, or write access.
        /// </summary>
        FILE_SHARE_NONE = 0x00000000,

        /// <summary>
        ///     Enables subsequent open operations on a file or device to request delete access.
        ///     Otherwise, other processes cannot open the file or device if they request delete access.
        ///     If this flag is not specified, but the file or device has been opened for delete access, the function
        ///     fails.
        ///     Note  Delete access allows both delete and rename operations.
        /// </summary>
        FILE_SHARE_DELETE = 0x00000004,

        /// <summary>
        ///     Enables subsequent open operations on a file or device to request read access.
        ///     Otherwise, other processes cannot open the file or device if they request read access.
        ///     If this flag is not specified, but the file or device has been opened for read access, the function
        ///     fails.
        /// </summary>
        FILE_SHARE_READ = 0x00000001,

        /// <summary>
        ///     Enables subsequent open operations on a file or device to request write access.
        ///     Otherwise, other processes cannot open the file or device if they request write access.
        ///     If this flag is not specified, but the file or device has been opened for write access or has a file mapping
        ///     with write access, the function fails.
        /// </summary>
        FILE_SHARE_WRITE = 0x00000002
    }
}