using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace KaldaygeduWalaineejaw
{
    public class AgileEncryptionInfo : EncryptionInfo
    {
        public AgileEncryptionInfo(VersionData encryptionVersionInfo, BinaryReader binaryReader) : base(encryptionVersionInfo)
        {
            Reserved = binaryReader.ReadUInt16();

            Debug.Assert(Reserved == 0x00000040);

            var byteList =
                binaryReader.ReadBytes((int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position));

            var text = Encoding.UTF8.GetString(byteList);
            text = text.Trim('\0');

            var xmlSerializer = new XmlSerializer(typeof(Encryption));
            var encryption = (Encryption)xmlSerializer.Deserialize(new XmlTextReader(new StringReader(text)));
            Encryption = encryption;
        }

        public Encryption Encryption { get; }

        private ushort Reserved { get; }
    }
}