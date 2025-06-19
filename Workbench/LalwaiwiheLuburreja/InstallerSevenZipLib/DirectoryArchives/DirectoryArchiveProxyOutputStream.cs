using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

class DirectoryArchiveProxyOutputStream : Stream
{
    public DirectoryArchiveProxyOutputStream(DirectoryInfo outputFolder)
    {
        _outputFolder = outputFolder;
    }

    private readonly DirectoryInfo _outputFolder;

    public override void Flush()
    {

    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var inputOffset = offset;
        var inputCount = count;
        _ = inputOffset;
        _ = inputCount;

        if (_headInfo is null)
        {
            long headLength = 0;
            for (int i = 0; i < sizeof(long); i++)
            {
                int v = buffer[offset + i];
                headLength |= ((long) (byte) v) << (8 * i);
            }

            _headInfo = new HeadInfo
            {
                HeadLength = headLength
            };

            if (count < headLength)
            {
                // 头部信息不完整，无法继续处理。不想跨越读取
                throw new Exception();
            }

            var contentLength = headLength - sizeof(long);
            var headStream = new MemoryStream(buffer, offset + sizeof(long), (int) contentLength);
            var streamReader = new StreamReader(headStream);
            while (streamReader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                Console.WriteLine(line);
                var relativePath = line.Trim();

                line = streamReader.ReadLine();
                if (line is null)
                {
                    break;
                }

                //$"Length={fileInfo.Length}"
                Match match = Regex.Match(line, @"Length=(\d+)");
                ReadOnlySpan<char> lengthText = match.Groups[1].ValueSpan;
                var length = long.Parse(lengthText);

                _headInfo.FileInfos.Add((relativePath, length));
            }

            offset += (int) headLength;
            count -= (int) headLength;

            var result = UpdateNextFile(_headInfo);
            if (!result)
            {
                return;
            }
        }

        if (_currentFileStream is null || _headInfo is null)
        {
            throw new InvalidOperationException();
        }

        while (count > 0)
        {
            var position = _currentFileStream.Position;
            var remindLength = _currentFileLength - position;

            var writeCount = Math.Min(count, (int) remindLength);
            _currentFileStream.Write(buffer, offset, writeCount);

            if (writeCount == remindLength)
            {
                _currentFileStream.Dispose();
                _currentFileStream = null;
                var result = UpdateNextFile(_headInfo);
                if (!result)
                {
                    return;
                }
            }

            count -= writeCount;
            offset += writeCount;
        }
    }

    
    [MemberNotNullWhen(true, nameof(_currentFileStream))]
    private bool UpdateNextFile(HeadInfo headInfo)
    {
        while (headInfo.CurrentFileIndex < headInfo.FileInfos.Count)
        {
            var (relativePath, length) = headInfo.FileInfos[headInfo.CurrentFileIndex];
            headInfo.CurrentFileIndex++;

            var filePath = Path.Join(_outputFolder.FullName, relativePath);

            var fileDirectory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("无法获取文件目录。");
            Directory.CreateDirectory(fileDirectory);

            _currentFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _currentFileStream.SetLength(length);

            _currentFileLength = length;
            if (length == 0)
            {
                _currentFileStream.Dispose();
                continue;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length { get; }
    public override long Position { get; set; }

    private FileStream? _currentFileStream;
    private long _currentFileLength = 0;

    private HeadInfo? _headInfo;

    class HeadInfo
    {
        public long HeadLength { get; set; }
        public List<(string RelativePath, long Length)> FileInfos { get; } = [];

        public int CurrentFileIndex { get; set; } = 0;
    }
}