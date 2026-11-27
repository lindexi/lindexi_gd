using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

namespace MediaFoundation.PlayerDemo;

internal static class MediaProbe
{
    internal static Task<string> RunAsync(Uri source, CancellationToken token, bool useFileUrl = false) =>
        RunAsync(source, token, useFileUrl ? "file-url" : "byte-stream");

    internal static Task<string> RunAsync(Uri source, CancellationToken token, string input) => Task.Run
        (() => Run(source, token, input), token);

    private static unsafe string Run(Uri source, CancellationToken token, string input)
    {
        bool useFileUrl = input != "byte-stream";
        var report = new StringBuilder();
        bool com = false, mf = false;
        bool complete = false;
        string stage = "CoInitializeEx";
        try
        {
            PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED).ThrowOnFailure();
            com = true;
            stage = "MFStartup";
            PInvoke.MFStartup(PInvoke.MF_VERSION, 0).ThrowOnFailure();
            mf = true;
            try
            {
                complete = ReadNative(source, report, token, input);
            }
            catch (COMException error)
            {
                ReportNativeFailure(report, "cleanup", error);
            }

            if (!complete && !useFileUrl && source.IsFile && Path.GetExtension(source.LocalPath).Equals
                    (".mkv", StringComparison.OrdinalIgnoreCase))
            {
                string tracks = MatroskaTracks.Read(source.LocalPath, token);
                report.Append(tracks);
                EnumerateContainerCodecs(tracks, report, token);
                complete = true;
            }
        }
        catch (Exception error) when (error is COMException or IOException or UnauthorizedAccessException
                                          or InvalidDataException)
        {
            ReportNativeFailure(report, mf ? "container fallback" : stage, error);
        }
        finally
        {
            if (mf) PInvoke.MFShutdown();
            if (com) PInvoke.CoUninitialize();
        }

        if (!complete && !useFileUrl && source.IsFile && Path.GetExtension(source.LocalPath).Equals
                (".mkv", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                report.Append(MatroskaTracks.Read(source.LocalPath, token));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException
                                              or OverflowException)
            {
                report.AppendLine($"EBML metadata HRESULT=0x{error.HResult:X8} {error.GetType().Name}");
            }
        }

        return report.ToString();
    }

    private static unsafe bool ReadNative(Uri source, StringBuilder report, CancellationToken token, string input)
    {
        IMFByteStream* bytes = null;
        IMFSourceResolver* resolver = null;
        Windows.Win32.System.Com.IUnknown* resolved = null;
        IMFMediaSource* media = null;
        IMFPresentationDescriptor* presentation = null;
        string stage = "MFCreateSourceResolver";
        try
        {
            PInvoke.MFCreateSourceResolver(&resolver).ThrowOnFailure();
            MF_OBJECT_TYPE kind;
            string[] parts = input.Split(':', 2);
            string? decode = parts.Length == 2 ? parts[0] : null;
            input = parts[^1];
            if (source.IsFile && (input is "byte-stream" or "byte-stream-mime" or "memory-stream"))
            {
                if (input == "memory-stream")
                {
                    stage = "SHCreateMemStream/MFCreateMFByteStreamOnStream";
                    using var file = File.OpenRead(source.LocalPath);
                    if (file.Length > 16 * 1024 * 1024)
                    {
                        report.AppendLine("outcome=skipped reason=memory-stream-size-limit");
                        return false;
                    }
                    byte[] data = new byte[checked((int)file.Length)];
                    file.ReadExactly(data);
                    fixed (byte* buffer = data)
                    {
                        var stream = PInvoke.SHCreateMemStream(buffer, (uint)data.Length);
                        if (stream == null) throw new OutOfMemoryException();
                        try { PInvoke.MFCreateMFByteStreamOnStream(stream, &bytes).ThrowOnFailure(); }
                        finally { stream->Release(); }
                    }
                }
                else
                {
                    stage = "MFCreateFile";
                    fixed (char* path = source.LocalPath)
                        PInvoke.MFCreateFile
                    (
                        MF_FILE_ACCESSMODE.MF_ACCESSMODE_READ, MF_FILE_OPENMODE.MF_OPENMODE_FAIL_IF_NOT_EXIST,
                        MF_FILE_FLAGS.MF_FILEFLAGS_NONE, new Windows.Win32.Foundation.PCWSTR(path), &bytes
                    ).ThrowOnFailure();
                }
                bytes->GetCapabilities(out uint capabilities);
                bytes->GetLength(out ulong length);
                bytes->GetCurrentPosition(out ulong position);
                report.AppendLine($"MF stream capabilities=0x{capabilities:X} length={length} position={position}");
                bool useMp4Mime = input == "byte-stream-mime";
                if (useMp4Mime)
                {
                    stage = "IMFByteStream.QueryInterface(IMFAttributes)/SetString(MF_BYTESTREAM_CONTENT_TYPE)";
                    Guid attributesId = typeof(IMFAttributes).GUID;
                    IMFAttributes* attributes = null;
                    ((Windows.Win32.System.Com.IUnknown*)bytes)->QueryInterface(&attributesId, (void**)&attributes).ThrowOnFailure();
                    try
                    {
                        attributes->SetString(PInvoke.MF_BYTESTREAM_CONTENT_TYPE, "video/mp4");
                    }
                    finally
                    {
                        attributes->Release();
                    }
                }
                string hint = "media" + Path.GetExtension(source.LocalPath);
                report.AppendLine
                (
                    $"MF resolver=CreateObjectFromByteStream contentTypeHint={(useMp4Mime ? "video/mp4" : "unset")} urlExtension=" + Path.GetExtension
                        (source.LocalPath)
                );
                stage = "IMFSourceResolver.CreateObjectFromByteStream";
                System.Diagnostics.Trace.WriteLine($"MF metadata begin stage={stage}", "Demo.Diagnostics");
                fixed (char* url = hint)
                    resolver->CreateObjectFromByteStream(bytes, url, 1, null, &kind, &resolved);
            }
            else
            {
                stage = "IMFSourceResolver.CreateObjectFromURL";
                report.AppendLine($"MF resolver=CreateObjectFromURL sourceType={(source.IsFile ? "file" : "network")}");
                string address = input == "local-path" ? source.LocalPath : source.AbsoluteUri;
                System.Diagnostics.Trace.WriteLine($"MF metadata begin stage={stage} input={input}", "Demo.Diagnostics");
                fixed (char* url = address)
                    resolver->CreateObjectFromURL(url, 1, null, &kind, &resolved);
            }

            Guid iid = typeof(IMFMediaSource).GUID;
            void* pointer = null;
            stage = "IUnknown.QueryInterface(IMFMediaSource)";
            resolved->QueryInterface(&iid, &pointer).ThrowOnFailure();
            media = (IMFMediaSource*)pointer;
            if (decode != null) return ReadFirstVideoFrame(media, report, decode == "decode-hardware-allowed");
            stage = "IMFMediaSource.CreatePresentationDescriptor";
            media->CreatePresentationDescriptor(&presentation);
            uint count;
            stage = "IMFPresentationDescriptor.GetStreamDescriptorCount";
            presentation->GetStreamDescriptorCount(&count);
            if (count > 256) throw new InvalidDataException();
            report.AppendLine($"MF streams={count}");
            bool complete = count > 0;
            for (uint i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                IMFStreamDescriptor* stream = null;
                IMFMediaTypeHandler* handler = null;
                IMFMediaType* type = null;
                stage = $"track={i}";
                try
                {
                    Windows.Win32.Foundation.BOOL selected;
                    presentation->GetStreamDescriptorByIndex(i, &selected, &stream);
                    stream->GetMediaTypeHandler(&handler);
                    // Enumerate the source's native compressed type, not a decoded output type.
                    handler->GetMediaTypeByIndex(0, &type);
                    Guid major, subtype;
                    type->GetGUID(PInvoke.MF_MT_MAJOR_TYPE, out major);
                    type->GetGUID(PInvoke.MF_MT_SUBTYPE, out subtype);
                    ulong size = 0;
                    uint channels = 0, rate = 0;
                    if (major == PInvoke.MFMediaType_Video) type->GetUINT64(PInvoke.MF_MT_FRAME_SIZE, out size);
                    if (major == PInvoke.MFMediaType_Audio)
                    {
                        type->GetUINT32(PInvoke.MF_MT_AUDIO_NUM_CHANNELS, out channels);
                        type->GetUINT32(PInvoke.MF_MT_AUDIO_SAMPLES_PER_SECOND, out rate);
                    }

                    Windows.Win32.Foundation.PWSTR language = default;
                    uint length;
                    string lang = "und";
                    try
                    {
                        stream->GetAllocatedString(PInvoke.MF_SD_LANGUAGE, out language, out length);
                        if (language.Value != null) lang = new string(language.Value);
                    }
                    catch (COMException error) when (error.HResult == unchecked((int)0xC00D36E6))
                    {
                        lang = "und (MF_SD_LANGUAGE absent)";
                    }
                    finally
                    {
                        PInvoke.CoTaskMemFree(language.Value);
                    }

                    report.AppendLine
                    (
                        $"MF track={i} selected={selected} major={major} subtype={subtype} codec={CodecName(subtype)} language={lang} width={size >> 32} height={size & uint.MaxValue} channels={channels} sampleRate={rate}"
                    );
                    if (major == PInvoke.MFMediaType_Video || major == PInvoke.MFMediaType_Audio)
                        EnumerateDecoders(major, subtype, report);
                }
                catch (COMException error)
                {
                    complete = false;
                    ReportNativeFailure(report, stage, error);
                }
                finally
                {
                    if (type != null) type->Release();
                    if (handler != null) handler->Release();
                    if (stream != null) stream->Release();
                }
            }

            return complete;
        }
        catch (Exception error) when (error is COMException or IOException or UnauthorizedAccessException)
        {
            ReportNativeFailure(report, stage, error);
            return false;
        }
        finally
        {
            if (presentation != null) presentation->Release();
            if (media != null)
            {
                media->Shutdown();
                media->Release();
            }

            if (resolved != null) resolved->Release();
            if (resolver != null) resolver->Release();
            // The resolved media source owns stream shutdown; a second Close can fail and mask the probe result.
            if (bytes != null) bytes->Release();
        }
    }

    private static unsafe bool ReadFirstVideoFrame(IMFMediaSource* media, StringBuilder report, bool allowHardware)
    {
        IMFAttributes* options = null;
        IMFSourceReader* reader = null;
        IMFMediaType* output = null;
        string stage = "MFCreateAttributes";
        try
        {
            PInvoke.MFCreateAttributes(&options, 2).ThrowOnFailure();
            stage = "IMFAttributes.SetUINT32";
            options->SetUINT32(PInvoke.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, allowHardware ? 1u : 0u);
            options->SetUINT32(PInvoke.MF_SOURCE_READER_DISABLE_DXVA, allowHardware ? 0u : 1u);
            stage = "MFCreateSourceReaderFromMediaSource";
            PInvoke.MFCreateSourceReaderFromMediaSource(media, options, &reader).ThrowOnFailure();
            stage = "MFCreateMediaType";
            PInvoke.MFCreateMediaType(&output).ThrowOnFailure();
            output->SetGUID(PInvoke.MF_MT_MAJOR_TYPE, PInvoke.MFMediaType_Video);
            output->SetGUID(PInvoke.MF_MT_SUBTYPE, PInvoke.MFVideoFormat_NV12);
            uint video = unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM);
            stage = "IMFSourceReader.SetCurrentMediaType(NV12)";
            reader->SetCurrentMediaType(video, null, output);
            stage = "IMFSourceReader.ReadSample";
            for (int i = 0; i < 64; i++)
            {
                IMFSample* sample = null;
                uint flags = 0;
                long timestamp = 0;
                reader->ReadSample(video, 0, null, &flags, &timestamp, &sample);
                if (sample != null)
                {
                    sample->Release();
                    report.AppendLine($"decodedFrame=true timestamp100ns={timestamp} flags=0x{flags:X} hardwareAllowed={allowHardware} actualDecoder=unknown");
                    return true;
                }
                if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0) break;
            }
            report.AppendLine("decodedFrame=false reason=no-sample");
            return false;
        }
        catch (COMException error)
        {
            ReportNativeFailure(report, stage, error);
            return false;
        }
        finally
        {
            if (output != null) output->Release();
            if (reader != null) reader->Release();
            if (options != null) options->Release();
        }
    }

    private static void ReportNativeFailure(StringBuilder report, string stage, Exception error)
    {
        string summary = $"MF metadata HRESULT=0x{error.HResult:X8} stage={stage} type={error.GetType().Name}";
        report.AppendLine(summary);
        System.Diagnostics.Trace.WriteLine
        (
            $"{summary}{Environment.NewLine}{PlaybackDiagnostics.Sanitize(error.ToString())}", "Demo.Diagnostics"
        );
    }

    private static unsafe void EnumerateContainerCodecs(string tracks, StringBuilder report, CancellationToken token)
    {
        foreach (string line in tracks.Split('\n').Where(line => line.StartsWith("EBML ", StringComparison.Ordinal)))
        {
            token.ThrowIfCancellationRequested();
            string? codec =
                line.Split(' ').FirstOrDefault(part => part.StartsWith("codec=", StringComparison.Ordinal))?[6..];
            Guid subtype = codec switch
            {
                "V_MPEG4/ISO/AVC" => new("34363248-0000-0010-8000-00aa00389b71"),
                "V_MPEGH/ISO/HEVC" => new("43564548-0000-0010-8000-00aa00389b71"),
                "V_VP9" => new("30395056-0000-0010-8000-00aa00389b71"),
                "V_AV1" => new("31305641-0000-0010-8000-00aa00389b71"),
                "A_AAC" => new("00001610-0000-0010-8000-00aa00389b71"),
                "A_MPEG/L3" => new("00000055-0000-0010-8000-00aa00389b71"),
                "A_AC3" => new("e06d802c-db46-11cf-b4d1-00805f6cbbea"),
                "A_EAC3" => new("a7fb87af-2d02-42fb-a4d4-05cd93843bdd"),
                "A_DTS" => new("00002001-0000-0010-8000-00aa00389b71"),
                _ => Guid.Empty
            };
            if (subtype == Guid.Empty)
            {
                report.AppendLine($"EBML codec={codec} MFT mapping=unknown (not evidence of missing decoder)");
                continue;
            }

            report.AppendLine($"EBML codec={codec} mappedSubtype={subtype}");
            EnumerateDecoders
            (
                codec != null && codec.StartsWith("V_", StringComparison.Ordinal)
                    ? PInvoke.MFMediaType_Video
                    : PInvoke.MFMediaType_Audio, subtype, report
            );
        }
    }

    private static string CodecName(Guid subtype)
    {
        uint code = BitConverter.ToUInt32(subtype.ToByteArray());
        return code switch
        {
            0x34363248 => "H.264", 0x43564548 or 0x35363248 => "HEVC", 0x30395056 => "VP9",
            0x31305641 => "AV1", 0x1610 => "AAC", 0x55 => "MP3", 0x2000 => "AC-3", 0x2001 => "DTS",
            _ when subtype == new Guid("e06d802c-db46-11cf-b4d1-00805f6cbbea") => "AC-3",
            _ when subtype == new Guid("a7fb87af-2d02-42fb-a4d4-05cd93843bdd") => "E-AC-3",
            _ => subtype.ToString()
        };
    }

    private static unsafe void EnumerateDecoders(Guid major, Guid subtype, StringBuilder report)
    {
        IMFActivate** activations = null;
        uint count = 0;
        var input = new MFT_REGISTER_TYPE_INFO { guidMajorType = major, guidSubtype = subtype };
        try
        {
            PInvoke.MFTEnumEx
            (
                major == PInvoke.MFMediaType_Video
                    ? PInvoke.MFT_CATEGORY_VIDEO_DECODER
                    : PInvoke.MFT_CATEGORY_AUDIO_DECODER,
                MFT_ENUM_FLAG.MFT_ENUM_FLAG_ALL, &input, null, &activations, &count
            ).ThrowOnFailure();
            report.AppendLine
            (
                $"MFT subtype={subtype} registeredCandidates={count} (registration is not proof of profile/bit-depth support)"
            );
            for (uint i = 0; i < count; i++)
            {
                Windows.Win32.Foundation.PWSTR name = default;
                uint length;
                try
                {
                    activations[i]->GetAllocatedString(PInvoke.MFT_FRIENDLY_NAME_Attribute, out name, out length);
                    Guid clsid = default;
                    activations[i]->GetGUID(PInvoke.MFT_TRANSFORM_CLSID_Attribute, out clsid);
                    uint hardwareLength = 0;
                    int hardwareResult = 0;
                    try
                    {
                        activations[i]->GetStringLength(PInvoke.MFT_ENUM_HARDWARE_URL_Attribute, out hardwareLength);
                    }
                    catch (COMException error) when (error.HResult == unchecked((int)0xC00D36E6))
                    {
                        hardwareResult = error.HResult;
                    }

                    report.AppendLine
                    (
                        $"MFT candidate={(name.Value == null ? "unknown" : new string(name.Value))} clsid={clsid} hardware={hardwareResult == 0} hardwareAttributeHRESULT=0x{hardwareResult:X8} (candidate, not selected decoder)"
                    );
                }
                finally
                {
                    PInvoke.CoTaskMemFree(name.Value);
                }
            }
        }
        finally
        {
            for (uint i = 0; i < count; i++)
                if (activations[i] != null)
                    activations[i]->Release();
            PInvoke.CoTaskMemFree(activations);
        }
    }
}