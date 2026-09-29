#if EMBED_NATIVE_LIBRARIES
using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace IpTrace;

internal static class NativeLibraryBootstrap
{
    internal static void Initialize()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        var assembly = typeof(NativeLibraryBootstrap).Assembly;
        foreach (var name in new[] { "libHarfBuzzSharp.dll", "libSkiaSharp.dll", "av_libglesv2.dll" })
        {
            LoadLibrary(assembly, name);
        }
    }

    private static void LoadLibrary(Assembly assembly, string name)
    {
        using var resource = assembly.GetManifestResourceStream($"IpTrace.Native.{name}")
            ?? throw new MissingManifestResourceException(name);
        var hash = SHA256.HashData(resource);
        var directory = Path.Combine(Path.GetTempPath(), "IpTrace", "native", Convert.ToHexString(hash));
        var path = Path.Combine(directory, name);
        var mutexName = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(path)));
        using var mutex = new Mutex(false, $"Local\\IpTrace.Native.{mutexName}");
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromSeconds(30));
            }
            catch (AbandonedMutexException)
            {
                // The previous process exited during extraction; verify or replace its cache.
                acquired = true;
            }

            if (!acquired)
            {
                throw new TimeoutException(path);
            }

            Directory.CreateDirectory(directory);
            if (!HasExpectedHash(path, hash))
            {
                var stagingPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");
                try
                {
                    resource.Position = 0;
                    using (var output = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        resource.CopyTo(output);
                        output.Flush(true);
                    }

                    File.Move(stagingPath, path, true);
                }
                finally
                {
                    File.Delete(stagingPath);
                }
            }

            // Keep the verified file protected from replacement while loading. Handles stay
            // loaded for the process lifetime because Avalonia retains native function pointers.
            using var verifiedFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(verifiedFile), hash))
            {
                throw new InvalidDataException(path);
            }

            NativeLibrary.Load(path);
        }
        finally
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static bool HasExpectedHash(string path, byte[] hash)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(file), hash);
    }
}
#endif
