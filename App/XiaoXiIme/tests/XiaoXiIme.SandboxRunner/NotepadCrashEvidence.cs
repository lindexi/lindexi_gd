using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace XiaoXiIme.SandboxRunner;

internal static class NotepadCrashEvidence
{
    internal static int Collect(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "notepad-crash-evidence.txt")) { AutoFlush = true };
        const uint timestamp = 0x6b02ccf3;
        const string reportId = "5a370d5b-5ab7-47ca-be51-bef4fdd735c2";
        log.WriteLine($"Machine={Environment.MachineName}; User={Environment.UserName}; ExpectedTimestamp=0x{timestamp:X8}; RVA=0x0021ED5C");
        InspectImage(Path.Combine(Environment.SystemDirectory, "XiaoXiIme.ime"));
        InspectImage(@"C:\lindexi\IME\native\ime\win-x64\XiaoXiIme.ime");
        foreach (var retired in Directory.EnumerateFiles(Environment.SystemDirectory, "XiaoXiIme.retired-*.ime"))
        {
            InspectImage(retired);
        }
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var boundary = Path.Combine(local, "XiaoXiIme", "Logs", "ime-boundary.log");
        log.WriteLine($"BoundaryLog={boundary}; Exists={File.Exists(boundary)}");
        if (File.Exists(boundary))
        {
            File.Copy(boundary, Path.Combine(output, "ime-boundary.log"), overwrite: true);
        }

        foreach (var root in new[] { Path.Combine(local, "Microsoft", "Windows", "WER"),
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows", "WER") })
        {
            foreach (var folder in new[] { "ReportArchive", "ReportQueue" })
            {
                var directory = Path.Combine(root, folder);
                if (!Directory.Exists(directory))
                {
                    continue;
                }
                try
                {
                    foreach (var report in Directory.EnumerateDirectories(directory, "AppCrash_Notepad*"))
                    {
                        var file = Path.Combine(report, "Report.wer");
                        if (!File.Exists(file) || !File.ReadAllText(file).Contains(reportId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        log.WriteLine($"MatchingReport={file}");
                        File.Copy(file, Path.Combine(output, "Report.wer"), overwrite: true);
                        foreach (var dump in Directory.EnumerateFiles(report, "*.dmp"))
                        {
                            log.WriteLine($"ExistingDump={dump}; bytes={new FileInfo(dump).Length}");
                        }
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    log.WriteLine($"WER scan failed for {directory}: {exception.Message}");
                }
            }
        }
        var dumpDirectory = Path.Combine(local, "CrashDumps");
        if (Directory.Exists(dumpDirectory))
        {
            foreach (var dump in Directory.EnumerateFiles(dumpDirectory, "Notepad.exe.*.dmp"))
            {
                log.WriteLine($"LocalDump={dump}; bytes={new FileInfo(dump).Length}");
            }
        }
        using var config = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Notepad.exe");
        log.WriteLine($"NotepadLocalDumpsConfigured={config is not null}; DumpFolder={config?.GetValue("DumpFolder")}");
        return 0;

        void InspectImage(string path)
        {
            if (!File.Exists(path))
            {
                log.WriteLine($"Image missing: {path}");
                return;
            }
            using var stream = File.OpenRead(path);
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            stream.Position = 0;
            using var pe = new PEReader(stream);
            var actual = unchecked((uint)pe.PEHeaders.CoffHeader.TimeDateStamp);
            log.WriteLine($"Image={path}; Timestamp=0x{actual:X8}; SHA256={hash}");
            if (actual != timestamp)
            {
                return;
            }
            File.Copy(path, Path.Combine(output, "faulting-XIAOXIIME.IME"), overwrite: true);
            foreach (var entry in pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.CodeView))
            {
                var codeView = pe.ReadCodeViewDebugDirectoryData(entry);
                log.WriteLine($"PDB={codeView.Path}; GUID={codeView.Guid}; Age={codeView.Age}");
            }
            var pdb = @"C:\lindexi\IME\native\ime\win-x64\XiaoXiIme.ImeModule.pdb";
            if (File.Exists(pdb))
            {
                log.WriteLine($"DeployedPdb={pdb}; bytes={new FileInfo(pdb).Length}");
            }
        }
    }
}
