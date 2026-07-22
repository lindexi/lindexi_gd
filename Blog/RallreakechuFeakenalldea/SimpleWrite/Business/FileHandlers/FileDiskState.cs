using System;
using System.IO;
using System.Security;

namespace SimpleWrite.Business.FileHandlers;

internal readonly record struct FileDiskState(long Length, DateTime LastWriteTimeUtc)
{
    public static bool TryRead(FileInfo fileInfo, out FileDiskState state)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        try
        {
            fileInfo.Refresh();
            if (!fileInfo.Exists)
            {
                state = default;
                return false;
            }

            state = new FileDiskState(fileInfo.Length, fileInfo.LastWriteTimeUtc);
            return true;
        }
        catch (IOException)
        {
            state = default;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            state = default;
            return false;
        }
        catch (SecurityException)
        {
            state = default;
            return false;
        }
    }
}
