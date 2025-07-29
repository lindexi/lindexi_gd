using Oxage.Wmf.Extensions;

using System.IO;
using System.Linq;
using System.Text;

namespace Oxage.Wmf.Records
{
    [WmfRecord(Type = RecordType.META_TEXTOUT, SizeIsVariable = true)]
    public class WmfTextoutRecord : WmfBinaryRecord
    {
        public WmfTextoutRecord() : base()
        {
        }

        public short StringLength
        {
            get;
            set;
        }

        public string GetText(Encoding encoding)
        {
            var text = encoding.GetString(TextByteArray);
            return text;
        }

        public string GetText(CharacterSet characterSet) => GetText(characterSet.ToEncoding());

        public byte[] TextByteArray
        {
            get => _textByteArray;
            set
            {
                _textByteArray = value;
                StringLength = (short) value.Length;
            }
        }

        private byte[] _textByteArray;


        public short YStart
        {
            get;
            set;
        }

        public short XStart
        {
            get;
            set;
        }

        protected Encoding StringEncoding
        {
            get
            {
                return WmfHelper.GetAnsiEncoding();
            }
        }

        public override void Read(BinaryReader reader)
        {
            this.StringLength = reader.ReadInt16();

            if (this.StringLength > 0)
            {
                byte[] textByteArray = reader.ReadBytes(this.StringLength);
                TextByteArray = textByteArray;
            }

            // String (variable): The size of this field MUST be a multiple of two. If StringLength is an odd number, then this field MUST be of a size greater than or equal to StringLength + 1. A variable-length string that specifies the text to be drawn. The string does not need to be null-terminated, because StringLength specifies the length of the string. The string is written at the location specified by the XStart and YStart fields. See section 2.3.3.5 for information about the encoding of the field.
            var isOdd = StringLength % 2 == 1;
            if (isOdd)
            {
                reader.ReadByte();
            }

            this.YStart = reader.ReadInt16();
            this.XStart = reader.ReadInt16();
        }

        public override void Write(BinaryWriter writer)
        {
            byte[] textByteArray = TextByteArray;
            int offset = (textByteArray.Length % 2 == 1 ? +1 : +0); //1 extra byte for odd-length string

            base.RecordSizeBytes = (uint) (6 /* RecordSize and RecordFunction */ + 2 + (textByteArray.Length + offset) + 2 + 2 /* Parameters */);
            base.Write(writer);

            writer.Write(this.StringLength > 0 ? this.StringLength : (short) (textByteArray.Length + offset));
            writer.Write(textByteArray);

            if (textByteArray.Length % 2 == 1)
            {
                //Write a dummy byte after odd-length string so the record aligns to 16-bit boundary
                writer.Write((byte) 0x00);
            }

            writer.Write(this.YStart);
            writer.Write(this.XStart);
        }

        protected override void Dump(StringBuilder builder)
        {
            base.Dump(builder);
            builder.AppendLine("StringLength: " + this.StringLength);
            builder.AppendLine("TextByteArray: " + string.Join(',', TextByteArray.Select(t => t.ToString("X2"))));
            builder.AppendLine("GuessText:" + WmfHelper.GetAnsiEncoding().GetString(TextByteArray));
            builder.AppendLine("YStart: " + this.YStart);
            builder.AppendLine("XStart: " + this.XStart);
        }
    }
}
