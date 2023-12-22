// See https://aka.ms/new-console-template for more information

using System.Globalization;

var text = """
           05 01 09 02 A1 01 85 02 09 01 A1 00 05 09 19 01
           29 10 15 00 25 01 35 00 45 01 65 00 55 00 75 01
           95 10 81 02 05 01 09 30 26 FF 07 45 00 75 0C 95
           01 81 06 09 31 81 06 09 38 25 7F 75 08 81 06 05
           0C 0A 38 02 81 06 C1 00 C1 00
           """;

using var fileStream = new FileStream("描述符信息.hid", FileMode.Create, FileAccess.Write);
for (var i = 0; i < text.Length - 1; i++)
{
    var c = text[i];
    if (c == ' ')
    {
        continue;
    }

    if (c is '\n' or '\r')
    {
        continue;
    }

    var span = text.AsSpan(i, 2);
    if (int.TryParse(span, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
    {
        fileStream.WriteByte((byte) value);
    }

    i++;
}
