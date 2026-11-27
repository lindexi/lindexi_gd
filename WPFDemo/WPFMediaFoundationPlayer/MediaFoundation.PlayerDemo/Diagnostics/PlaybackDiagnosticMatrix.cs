using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32;
using MediaFoundation.Controls;

namespace MediaFoundation.PlayerDemo;

internal static class PlaybackDiagnosticMatrix
{
    private const long CopyLimit = 128 * 1024 * 1024;
    private static readonly string[] s_inputs = ["byte-stream", "byte-stream-mime", "memory-stream", "local-path", "file-url"];

    internal static async Task<string?> RunAsync(Uri source, string failedMode, Exception? failure, CancellationToken token, string? firstFrameEvidence = null)
    {
        if (!source.IsFile) return null;
        string directory = Path.GetDirectoryName(PlaybackDiagnostics.LogPath)!;
        string report = Path.ChangeExtension(PlaybackDiagnostics.LogPath, $"diagnostics-{Guid.NewGuid():N}.txt");
        string root = Path.Combine(Path.GetTempPath(), $"mfdiag-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(directory);
            Directory.CreateDirectory(root);
            Trace.WriteLine($"DiagnosticMatrix report={report}", "Demo.Diagnostics");
            Write(report, $"DiagnosticMatrix v1 OS={Environment.OSVersion.Version} process={RuntimeInformation.ProcessArchitecture} OSArch={RuntimeInformation.OSArchitecture} runtime={RuntimeInformation.FrameworkDescription}");
            if (firstFrameEvidence != null) Write(report, $"firstFrameTimeout mode={failedMode} {firstFrameEvidence}");
            for (Exception? error = failure; error != null; error = error.InnerException)
                Write(report, $"originalFailure mode={failedMode} HRESULT=0x{error.HResult:X8} type={error.GetType().Name}{Environment.NewLine}{error.StackTrace}");
            string? exe = Environment.ProcessPath;
            if (exe == null || Path.GetFileName(exe).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                Write(report, "matrix outcome=skipped reason=published-apphost-required");
                return report;
            }

            string extension = Path.GetExtension(source.LocalPath);
            string copy = Path.Combine(root, "original" + extension);
            Write(report, "case=original test=environment");
            await RunCaseAsync(exe, report, "environment", source.LocalPath, token);
            Write(report, "case=original test=file-evidence");
            await RunCaseAsync(exe, report, "file-evidence", source.LocalPath, token);
            await RunMatrixAsync(exe, "original", source.LocalPath);
            Write(report, "case=baseline-mp4 outcome=skipped reason=no-mp4-bundled");
            string wav = Path.Combine(root, "baseline.wav");
            WriteWaveBaseline(wav);
            Write(report, "case=baseline-wav test=local-path");
            await RunCaseAsync(exe, report, "local-path", wav, token);
            Write(report, "case=baseline-wav test=byte-stream");
            await RunCaseAsync(exe, report, "byte-stream", wav, token);

            if (!Path.IsPathFullyQualified(copy) || copy.Any(c => c > 127))
                Write(report, "case=ascii-copy outcome=skipped reason=temp-path-not-ascii");
            else
            {
                Write(report, "case=ascii-copy test=copy-and-hash");
                bool copied = await RunCaseAsync(exe, report, "copy-to:" + copy, source.LocalPath, token);
                if (copied) await RunMatrixAsync(exe, "ascii-copy", copy);
            }

            string? peer = Environment.Is64BitOperatingSystem
                ? await ExtractAsync("Diagnostics.Peer.exe", Path.Combine(root, "MediaFoundation.PlayerDemo.exe"), token)
                : null;
            if (peer != null)
            {
                Write(report, "case=peer test=environment");
                await RunCaseAsync(peer, report, "environment", source.LocalPath, token);
                await RunMatrixAsync(peer, "peer-original", source.LocalPath);
            }
            else Write(report, $"case=peer outcome=skipped reason={(Environment.Is64BitOperatingSystem ? "peer-not-bundled" : "32-bit-os")}");

            Write(report, "coverage native-only=not-run reason=native-host-not-bundled");
            Write(report, "coverage system-etw=not-run reason=system-tracing-not-enabled");
            Write(report, "coverage wpf-final-presentation=not-run reason=isolated-surface-only");
            Write(report, "coverage non-single-file=not-run reason=unbundled-host-not-bundled");
            Write(report, "DiagnosticMatrix end");
            Trace.WriteLine($"DiagnosticMatrix complete report={report}", "Demo.Diagnostics");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Trace.WriteLine("DiagnosticMatrix canceled; partial report retained", "Demo.Diagnostics");
        }
        catch (Exception error) when (IsDiagnosticError(error))
        {
            Trace.WriteLine($"DiagnosticMatrix failure type={error.GetType().Name} HRESULT=0x{error.HResult:X8}", "Demo.Diagnostics");
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (IOException error) { Trace.WriteLine($"DiagnosticMatrix cleanup HRESULT=0x{error.HResult:X8}", "Demo.Diagnostics"); }
            catch (UnauthorizedAccessException error) { Trace.WriteLine($"DiagnosticMatrix cleanup HRESULT=0x{error.HResult:X8}", "Demo.Diagnostics"); }
        }

        return File.Exists(report) ? report : null;

        async Task RunMatrixAsync(string host, string label, string file)
        {
            string? resolvedInput = null;
            foreach (string input in s_inputs)
            {
                Write(report, $"case={label} test={input}");
                if (input == "byte-stream-mime" && !Path.GetExtension(file).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    Write(report, "outcome=skipped reason=not-mp4");
                    continue;
                }
                if (await RunCaseAsync(host, report, input, file, token)) resolvedInput ??= input;
            }
            foreach (string test in new[] { "mfplay-hwnd-byte", "mfplay-hwnd-path", "mfplay-d3dimage-byte", "mfplay-d3dimage-path", "mfplay-d3dimage-presentation-byte" })
            {
                Write(report, $"case={label} test={test}");
                await RunCaseAsync(host, report, test, file, token);
            }
            foreach (string test in new[] { "decode-software", "decode-hardware-allowed" })
            {
                Write(report, $"case={label} test={test} input={resolvedInput ?? "none"}");
                if (resolvedInput == null) Write(report, "outcome=skipped reason=no-media-source");
                else await RunCaseAsync(host, report, $"{test}:{resolvedInput}", file, token);
            }
        }
    }

    private static void WriteWaveBaseline(string path)
    {
        const int rate = 16000;
        const int bytes = rate;
        using var output = new BinaryWriter(File.Create(path));
        output.Write("RIFF"u8);
        output.Write(36 + bytes);
        output.Write("WAVEfmt "u8);
        output.Write(16);
        output.Write((short)1);
        output.Write((short)1);
        output.Write(rate);
        output.Write(rate * 2);
        output.Write((short)2);
        output.Write((short)16);
        output.Write("data"u8);
        output.Write(bytes);
        output.Write(new byte[bytes]);
    }

    private static async Task<string?> ExtractAsync(string name, string destination, CancellationToken token)
    {
        await using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
        if (resource == null) return null;
        await using var output = File.Create(destination);
        await resource.CopyToAsync(output, token);
        return destination;
    }

    private static async Task<bool> RunCaseAsync(string exe, string report, string test, string file, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string label = test.StartsWith("copy-to:", StringComparison.Ordinal) ? "copy-and-hash" : test;
        var start = new ProcessStartInfo(exe) { UseShellExecute = false, CreateNoWindow = true };
        foreach (string arg in new[] { "--diagnostic-worker", report, test, file }) start.ArgumentList.Add(arg);
        var elapsed = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(test == "environment" ? 25 : 10));
        try
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException();
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); }
                catch (InvalidOperationException) when (process.HasExited) { }
                using var termination = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                try { await process.WaitForExitAsync(termination.Token); }
                catch (OperationCanceledException) { Write(report, $"test={label} workerTermination=unconfirmed"); }
                Write(report, $"test={label} outcome={(token.IsCancellationRequested ? "canceled" : "timeout")} elapsedMs={elapsed.ElapsedMilliseconds}");
                token.ThrowIfCancellationRequested();
                return false;
            }
            string outcome = process.ExitCode switch { 0 => "pass", 1 => "failed", 2 => "skipped", _ => "worker-crash" };
            Write(report, $"test={label} outcome={outcome} workerExit=0x{process.ExitCode:X8} elapsedMs={elapsed.ElapsedMilliseconds}");
            return process.ExitCode == 0;
        }
        catch (Exception error) when (IsDiagnosticError(error))
        {
            Write(report, $"test={label} outcome=worker-error type={error.GetType().Name} HRESULT=0x{error.HResult:X8} elapsedMs={elapsed.ElapsedMilliseconds}");
            return false;
        }
    }

    internal static async Task<int> RunWorkerAsync(string report, string test, string file)
    {
        using var trace = new WorkerTraceListener(report, file);
        Trace.Listeners.Add(trace);
        try
        {
            Write(report, $"worker process={RuntimeInformation.ProcessArchitecture}");
            if (test == "environment") WriteEnvironment(report);
            else if (test == "file-evidence") WriteFileEvidence(report, file);
            else if (test.StartsWith("copy-to:", StringComparison.Ordinal))
            {
                using var source = File.OpenRead(file);
                if (source.Length > CopyLimit)
                {
                    Write(report, "outcome=skipped reason=copy-size-limit");
                    return 2;
                }
                long length = source.Length;
                string expected = Convert.ToHexString(await SHA256.HashDataAsync(source));
                source.Position = 0;
                string target = test[8..];
                await using (var destination = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await source.CopyToAsync(destination);
                using var copy = File.OpenRead(target);
                string actual = Convert.ToHexString(await SHA256.HashDataAsync(copy));
                bool matches = length == copy.Length && expected == actual;
                Write(report, $"outcome={(matches ? "pass" : "failed")} length={length} sha256={actual} copyVerified={matches}");
                return matches ? 0 : 1;
            }
            else if (test.StartsWith("mfplay-", StringComparison.Ordinal))
            {
                var source = new Uri(file);
                var mode = test.StartsWith("mfplay-hwnd-", StringComparison.Ordinal) ? VideoRenderingMode.Hwnd : VideoRenderingMode.D3DImage;
                bool opened = await PlaybackFailureProbe.ProbeAlternateModeAsync(source, mode,
                    message => Write(report, message.Replace(file, "[file]", StringComparison.OrdinalIgnoreCase)
                        .Replace(source.AbsoluteUri, "[file]", StringComparison.OrdinalIgnoreCase)),
                    CancellationToken.None, test.EndsWith("-path", StringComparison.Ordinal), mode == VideoRenderingMode.D3DImage,
                    test == "mfplay-d3dimage-presentation-byte" ? MediaFoundation.Core.VideoScalingMode.PresentationSurface : MediaFoundation.Core.VideoScalingMode.MixerOutput);
                return opened ? 0 : 1;
            }
            else if (s_inputs.Contains(test, StringComparer.Ordinal) || test.StartsWith("decode-", StringComparison.Ordinal))
            {
                var source = new Uri(file);
                string result = await MediaProbe.RunAsync(source, CancellationToken.None, test);
                Write(report, result.Replace(source.AbsoluteUri, "[file]", StringComparison.OrdinalIgnoreCase)
                    .Replace(file, "[file]", StringComparison.OrdinalIgnoreCase));
                if (result.Contains("outcome=skipped", StringComparison.Ordinal)) return 2;
                bool success = test.StartsWith("decode-", StringComparison.Ordinal)
                    ? result.Contains("decodedFrame=true", StringComparison.Ordinal)
                    : result.Contains("MF streams=", StringComparison.Ordinal);
                using var process = Process.GetCurrentProcess();
                foreach (ProcessModule module in process.Modules)
                    if (module.ModuleName.StartsWith("mf", StringComparison.OrdinalIgnoreCase) ||
                        module.ModuleName is "msmpeg2vdec.dll" or "d3d11.dll" or "d3d9.dll" or "dxgi.dll")
                        Write(report, $"loadedModule={module.ModuleName} version={module.FileVersionInfo.FileVersion}");
                Write(report, $"outcome={(success ? "pass" : "failed")}");
                return success ? 0 : 1;
            }
            else return 2;
            return 0;
        }
        catch (Exception error) when (IsDiagnosticError(error))
        {
            Write(report, $"outcome=failed type={error.GetType().Name} HRESULT=0x{error.HResult:X8}{Environment.NewLine}{error.StackTrace}");
            return 1;
        }
        finally
        {
            Trace.Listeners.Remove(trace);
        }
    }

    private sealed class WorkerTraceListener(string report, string file) : TraceListener
    {
        public override void Write(string? message) => WriteLine(message);

        public override void WriteLine(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (Uri.TryCreate(file, UriKind.Absolute, out var source))
                message = message.Replace(source.AbsoluteUri, "[file]", StringComparison.OrdinalIgnoreCase);
            if (file.Length > 0) message = message.Replace(file, "[file]", StringComparison.OrdinalIgnoreCase);
            PlaybackDiagnosticMatrix.Write(report, message);
        }
    }

    private static void WriteEnvironment(string report)
    {
        using var version = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        Write(report, $"edition={version?.GetValue("EditionID")} build={Environment.OSVersion.Version} UBR={version?.GetValue("UBR")}");
        foreach (string name in new[] { "mf.dll", "mfplat.dll", "mfplay.dll", "mfreadwrite.dll", "mfmp4srcsnk.dll", "msmpeg2vdec.dll" })
        {
            string path = Path.Combine(Environment.SystemDirectory, name);
            Write(report, $"component={name} version={(File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : "missing")}");
        }
        using var display = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
        foreach (string key in display?.GetSubKeyNames().Where(name => name.Length == 4 && name.All(char.IsAsciiDigit)) ?? [])
        {
            using var adapter = display?.OpenSubKey(key);
            Write(report, $"displayAdapter driverDescription={adapter?.GetValue("DriverDesc")} driverVersion={adapter?.GetValue("DriverVersion")}");
        }
        foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        foreach (var scope in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            using var hive = RegistryKey.OpenBaseKey(scope, view);
            foreach (string selector in new[] { ".mp4", "video/mp4" })
            {
                using var handlers = hive.OpenSubKey($@"SOFTWARE\Microsoft\Windows Media Foundation\ByteStreamHandlers\{selector}");
                string[] ids = handlers?.GetValueNames().Where(name => Guid.TryParse(name, out _)).ToArray() ?? [];
                Write(report, $"mp4Handlers scope={scope} registryView={view} selector={selector} keyPresent={handlers != null} CLSIDs={string.Join(",", ids)}");
                foreach (string id in ids)
                {
                    using var server = hive.OpenSubKey($@"SOFTWARE\Classes\CLSID\{id}\InprocServer32");
                    var registration = server?.GetValue(null) as string;
                    string? module = registration == null ? null : Environment.ExpandEnvironmentVariables(registration.Trim('"'));
                    Write(report, $"handlerCLSID={id} moduleName={Path.GetFileName(module)} registeredHere={server != null}");
                }
            }
        }
    }

    private static void WriteFileEvidence(string report, string path)
    {
        using var file = File.OpenRead(path);
        var drive = new DriveInfo(Path.GetPathRoot(path) ?? path);
        Write(report, $"driveType={drive.DriveType}");
        Write(report, $"file bytes={file.Length} attributes={File.GetAttributes(path)} pathLength={path.Length} ascii={path.All(c => c <= 127)}");
        Span<byte> sample = stackalloc byte[64];
        foreach (long offset in new[] { 0L, file.Length / 2, Math.Max(0, file.Length - sample.Length) })
        {
            file.Position = offset;
            Write(report, $"randomRead offset={offset} bytes={file.Read(sample)}");
        }
        if (!Path.GetExtension(path).Equals(".mp4", StringComparison.OrdinalIgnoreCase)) return;
        file.Position = 0;
        if (file.Length <= CopyLimit) Write(report, $"file sha256={Convert.ToHexString(SHA256.HashData(file))}");
        int boxes = 0;
        WriteMp4Boxes(file, report, 0, file.Length, "", 0, ref boxes);
        if (boxes >= 256) Write(report, "mp4BoxScan outcome=partial reason=box-limit");
    }

    private static void WriteMp4Boxes(Stream file, string report, long start, long end, string parent, int depth, ref int boxes)
    {
        Span<byte> sample = stackalloc byte[16];
        file.Position = start;
        while (boxes < 256 && file.Position < end)
        {
            boxes++;
            if (end - file.Position < 8)
            {
                Write(report, $"mp4Box parent={parent} offset={file.Position} valid=false reason=truncated-header");
                break;
            }
            long offset = file.Position;
            file.ReadExactly(sample[..8]);
            ulong size = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(sample);
            string type = new(sample[4..8].ToArray().Select(c => c is >= 32 and <= 126 ? (char)c : '?').ToArray());
            int header = 8;
            if (size == 1)
            {
                file.ReadExactly(sample[..8]);
                size = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(sample);
                header = 16;
            }
            else if (size == 0) size = (ulong)(end - offset);
            bool valid = size >= (ulong)header && size <= (ulong)(end - offset);
            Write(report, $"mp4Box parent={parent} type={type} offset={offset} bytes={size} valid={valid}");
            if (!valid) break;
            long next = offset + (long)size;
            if (depth < 6 && type is "moov" or "trak" or "mdia" or "minf" or "stbl" or "edts" or "mvex" or "moof" or "traf")
                WriteMp4Boxes(file, report, offset + header, next, $"{parent}/{type}", depth + 1, ref boxes);
            else if (type == "stsd" && size >= (ulong)(header + 16))
            {
                file.ReadExactly(sample);
                uint entries = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(sample[4..]);
                string first = new(sample[12..].ToArray().Select(c => c is >= 32 and <= 126 ? (char)c : '?').ToArray());
                Write(report, $"mp4SampleDescription entryCount={entries} firstEntry={first}");
            }
            file.Position = next;
        }
    }

    private static void Write(string report, string message) =>
        File.AppendAllText(report, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");

    private static bool IsDiagnosticError(Exception error) => error is IOException or UnauthorizedAccessException
        or InvalidOperationException or ArgumentException or ExternalException or System.Security.SecurityException
        or NotSupportedException;
}
