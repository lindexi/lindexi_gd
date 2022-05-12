var hashBytes = new byte[] { 0x01, 0x02, 0xFF, 0x00, 0x06 };
Console.WriteLine(string.Join("", hashBytes.Select(b => b.ToString("x2"))));
