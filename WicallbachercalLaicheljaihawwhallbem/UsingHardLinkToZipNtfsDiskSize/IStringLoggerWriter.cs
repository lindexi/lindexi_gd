namespace UsingHardLinkToZipNtfsDiskSize;

public interface IStringLoggerWriter : IAsyncDisposable
{
    ValueTask WriteAsync(string message);
}