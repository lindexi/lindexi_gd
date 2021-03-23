using System;
using System.IO;
using System.Text;

namespace KaldaygeduWalaineejaw
{
    public class LengthPrefixedUTF8String
    {
        public LengthPrefixedUTF8String(BinaryReader binaryReader)
        {
            var length = binaryReader.ReadInt32();
            Length = length;

            var byteList = binaryReader.ReadBytes(length);
            Text = Encoding.UTF8.GetString(byteList);
            if (length % 4 == 2)
            {
                /*
                 * Padding (variable): A set of bytes that MUST be of the correct size such that the size of the UNICODE-LP-P4 structure is a multiple of 4 bytes. If Padding is present, it MUST be exactly 2 bytes long, and each byte MUST be 0x00. 
                 */
                var paddingLength = 2;
                _ = binaryReader.ReadBytes(paddingLength);
            }
        }

        public Int32 Length { get; }
        public string Text { get; }
    }
}