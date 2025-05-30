// See https://aka.ms/new-console-template for more information

using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;

System.Text.Json.Utf8JsonReader reader = new Utf8JsonReader();

var jsonReader = JsonReaderWriterFactory.CreateJsonReader(new Fxx(new MemoryStream()),Encoding.UTF8, XmlDictionaryReaderQuotas.Max,
    dictionaryReader =>
    {

    });
Console.WriteLine("Hello, World!");

class Fxx : Stream
{
    public Fxx(MemoryStream memoryStream)
    {
        MemoryStream = memoryStream;
    }

    public MemoryStream MemoryStream { get; }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
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
        throw new NotImplementedException();
    }

    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }
    public override long Position { get; set; }
}
