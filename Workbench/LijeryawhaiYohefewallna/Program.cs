// See https://aka.ms/new-console-template for more information

using System.Text;

UInt16 早 = '早';
Span<byte> 早Span = stackalloc byte[sizeof(UInt16)];
BitConverter.TryWriteBytes(早Span, 早);
var bytes = Encoding.Unicode.GetBytes(['早']);
早Span.SequenceEqual(bytes);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Span<int> encodingCodePage = [936, 10008, 20936, 52936];
foreach (var codePage in encodingCodePage)
{
    var encoding = Encoding.GetEncoding(codePage);
    var 早Gbk = encoding.GetString(早Span);
    var 欲 = encoding.GetBytes(['欲']);
    Console.WriteLine(早Gbk);
}

