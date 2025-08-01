// See https://aka.ms/new-console-template for more information

using System.Text;

UInt16 早 = '早';
Span<byte> 早Span = stackalloc byte[sizeof(UInt16)];
BitConverter.TryWriteBytes(早Span, 早);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var encoding = Encoding.GetEncoding(52936);
var 早Gbk = encoding.GetString(早Span);
var 欲 = encoding.GetBytes(['欲']);
Console.WriteLine(早Gbk);
