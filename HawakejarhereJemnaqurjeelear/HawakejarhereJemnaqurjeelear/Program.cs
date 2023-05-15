using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace HawakejarhereJemnaqurjeelear;

internal class Program
{
    [DllImport("Kernel32")]
    private static extern void Wow64EnableWow64FsRedirection(bool enable);

    static void Main(string[] args)
    {
        Wow64EnableWow64FsRedirection(false);

        var webpCodecFolder = Path.GetFullPath("WebpCodec");
        var originX86Dll = Path.Combine(webpCodecFolder, "x86", "WebpWICCodec.dll");
        var originX64Dll = Path.Combine(webpCodecFolder, "x64", "WebpWICCodec.dll");

        if (Environment.Is64BitOperatingSystem)
        {
            // C:\Windows\System32\WebpWICCodec.dll
            var x64Dll = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\System32\WebpWICCodec.dll");
            RegisterX64(x64Dll);

            // C:\Windows\SysWOW64\WebpWICCodec.dll
            var x86Dll = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\SysWOW64\WebpWICCodec.dll");
            RegisterX86(x86Dll);
        }
        else
        {
            // C:\Windows\System32\WebpWICCodec.dll
            var x86Dll = Environment.ExpandEnvironmentVariables(@"%SystemDrive%\Windows\System32\WebpWICCodec.dll");
            RegisterX86(x86Dll);
        }

        void RegisterX64(string x64Dll)
        {
            File.Copy(originX64Dll, x64Dll);
            using var classesRootKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
            AddWebpToRegister(classesRootKey, x64Dll);
        }

        void RegisterX86(string x86Dll)
        {
            File.Copy(originX86Dll, x86Dll);

            using var classesRoot32Key = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32);

            AddWebpToRegister(classesRoot32Key, x86Dll);
        }
    }

    private static void AddWebpToRegister(RegistryKey classesRootKey, string webpDllFile)
    {
        using var webpRegisterKey =
            classesRootKey.CreateSubKey(
                @"CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance\{C747A836-4884-47B8-8544-002C41BD63D2}",
                RegistryKeyPermissionCheck.ReadWriteSubTree);
        webpRegisterKey.SetValue("CLSID", "{C747A836-4884-47B8-8544-002C41BD63D2}", RegistryValueKind.String);
        webpRegisterKey.SetValue("FriendlyName", "WebP Decoder", RegistryValueKind.String);

        using var webpDecoderKey = classesRootKey.CreateSubKey(@"CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}",
            RegistryKeyPermissionCheck.ReadWriteSubTree);
        webpDecoderKey.SetValue(null, "WebP Decoder", RegistryValueKind.String);
        webpDecoderKey.SetValue("ContainerFormat", "{1F122879-EBA0-4670-98C5-CF29F3B98711}", RegistryValueKind.String);
        webpDecoderKey.SetValue("FileExtensions", ".webp", RegistryValueKind.String);
        webpDecoderKey.SetValue("FriendlyName", "WebP Decoder", RegistryValueKind.String);
        webpDecoderKey.SetValue("MimeTypes", "image/webp", RegistryValueKind.String);
        webpDecoderKey.SetValue("VendorGUID", "{D4837961-2609-4B94-A9CB-A42A209AA021}", RegistryValueKind.String);

        // reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Formats\{6FDDC324-4E03-4BFE-B185-3D77768DC90F} /d 0x10 /f
        using var formatKey =
            classesRootKey.CreateSubKey(
                @"CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Formats\{6FDDC324-4E03-4BFE-B185-3D77768DC90F}");
        formatKey.SetValue(null, "0x10", RegistryValueKind.String);

        // reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\InProcServer32 /d %~dp0x64\WebpWICCodec.dll /f
        using var inProcServer32Key =
            classesRootKey.CreateSubKey(@"CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\InProcServer32");
        inProcServer32Key.SetValue(null, webpDllFile, RegistryValueKind.String);

        // reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\InProcServer32 /v ThreadingModel /d Both /f
        inProcServer32Key.SetValue("ThreadingModel", "Both", RegistryValueKind.String);

        // reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Patterns\0 /v Length /t REG_DWORD /d 12 /f
        using var patternsKey = classesRootKey.CreateSubKey(@"CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Patterns\0");
        patternsKey.SetValue("Length", 12, RegistryValueKind.DWord);

        //  reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Patterns\0 /v Mask /t REG_BINARY /d ffffffff00000000ffffffff /f
        patternsKey.SetValue("Mask", StringToByteArray("ffffffff00000000ffffffff"), RegistryValueKind.Binary);

        // reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Patterns\0 /v Pattern /t REG_BINARY /d 524946460000000057454250 /f
        patternsKey.SetValue("Pattern", StringToByteArray("524946460000000057454250"), RegistryValueKind.Binary);
        // reg add HKCR\CLSID\{C747A836-4884-47B8-8544-002C41BD63D2}\Patterns\0 /v Position /t REG_DWORD /d 0 /f
        patternsKey.SetValue("Position", 0, RegistryValueKind.DWord);
    }

    static byte[] StringToByteArray(string hex)
    {
        var length = hex.Length / 2;
        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
        {
            buffer[i] = byte.Parse(hex.AsSpan().Slice(i * 2, 2), NumberStyles.HexNumber);
        }

        return buffer;
        //return Enumerable.Range(0, hex.Length / 2).Select(x => byte.Parse(hex.AsSpan().Slice(x * 2, 2), NumberStyles.HexNumber))
        //    .ToArray();
    }
}
