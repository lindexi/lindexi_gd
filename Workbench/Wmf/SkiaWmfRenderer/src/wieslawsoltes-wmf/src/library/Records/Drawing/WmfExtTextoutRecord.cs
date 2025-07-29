using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Oxage.Wmf.Extensions;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
    //NOTE: The attribute is commented because the records is not fully implemented and thus wont be present while parsing WMF document
    [WmfRecord(Type = RecordType.META_EXTTEXTOUT, SizeIsVariable = true)]
    public class WmfExtTextoutRecord : WmfBinaryRecord
    {
        public WmfExtTextoutRecord() : base()
        {
        }

        public short Y
        {
            get;
            set;
        }

        public short X
        {
            get;
            set;
        }

        public short StringLength
        {
            get;
            set;
        }

        public ExtTextOutOptions FwOpts
        {
            get;
            set;
        }

        public WmfRect? Rectangle
        {
            get;
            set;
        }

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

        //public string StringValue
        //{
        //	get;
        //	set;
        //}

        public UInt16[]? Dx
        {
            get;
            set;
        }

        //protected Encoding StringEncoding
        //{
        //	get
        //	{
        //		return WmfHelper.GetAnsiEncoding();
        //	}
        //}

        public string GetText(Encoding encoding)
        {
            var text = encoding.GetString(TextByteArray);
            if (Dx != null && Dx.Length != text.Length)
            {
                throw new WmfException($"Dx length mush equals text length.");
            }

            return text;
        }

        public string GetText(CharacterSet characterSet) => GetText(characterSet.ToEncoding());

        public override void Read(BinaryReader reader)
        {
            this.Y = reader.ReadInt16();
            this.X = reader.ReadInt16();
            this.StringLength = reader.ReadInt16();
            this.FwOpts = (ExtTextOutOptions) reader.ReadUInt16();

            // Rectangle (8 bytes): An optional 8-byte Rect Object (section 2.2.2.18).) When either ETO_CLIPPED, ETO_OPAQUE, or both are specified, the rectangle defines the dimensions, in logical coordinates, used for clipping, opaquing, or both. When neither ETO_CLIPPED nor ETO_OPAQUE is specified, the coordinates in Rectangle are ignored.
            if (FwOpts is ExtTextOutOptions.ETO_CLIPPED or ExtTextOutOptions.ETO_OPAQUE)
            {
                this.Rectangle = new WmfRect();
                this.Rectangle.Read(reader);
            }

            // String (variable): A variable-length string that specifies the text to be drawn. The string does not need to be null-terminated, because StringLength specifies the length of the string. If the length is odd, an extra byte is placed after it so that the following member (optional Dx) is aligned on a 16-bit boundary. The string will be decoded based on the font object currently selected into the playback device context. If a font matching the font object’s specification is not found, the decoding is undefined. If a matching font is found that matches the charset specified in the font object, the string should be decoded with the codepages in the following table.
            byte[] textByteArray = reader.ReadBytes(this.StringLength);
            TextByteArray = textByteArray;

            var currentTakeByteCount = sizeof(UInt16) * (1/*Y*/ + 1/*X*/+ 1/*StringLength*/+ 1/*fwOpts*/)
                + (this.Rectangle != null ? 8/*Sizeof(Rect)*/ : 0)
                + StringLength;

            // Dx (variable): An optional array of 16-bit signed integers that indicate the distance between origins of adjacent character cells. For example, Dx[i] logical units separate the origins of character cell i and character cell i + 1. If this field is present, there MUST be the same number of values as there are characters in the string.

            var dxLength = RecordDataSizeBytes - currentTakeByteCount;
            // > String (variable): If the length is odd, an extra byte is placed after it so that the following member (optional Dx) is aligned on a 16-bit boundary.
            var isOdd = dxLength > ((dxLength / sizeof(UInt16)) * sizeof(UInt16));
            if (isOdd)
            {
                // 读取掉这个额外的字节，以便 Dx 对齐到 16 位边界
                var r = reader.ReadByte();
                _ = r;
                dxLength--;
            }

            UInt16[] dxArray = new UInt16[dxLength / sizeof(UInt16)];
            for (var i = 0; i < dxArray.Length; i++)
            {
                dxArray[i] = reader.ReadUInt16();
            }
            Dx = dxArray;
        }

        public override void Write(BinaryWriter writer)
        {
            int dxLength = (this.Dx != null ? this.Dx.Length * sizeof(UInt16) : 0);

            byte[] textByteArray = TextByteArray;

            var recordSizeBytes = (uint) (SizeofRecordSizeAndRecordTypeBytes /* RecordSize and RecordFunction */ + sizeof(UInt16) * (1/*Y*/ + 1/*X*/+ 1/*StringLength*/+ 1/*fwOpts*/)
                + (this.Rectangle != null ? 8/*Sizeof(Rect)*/ : 0) + textByteArray.Length + dxLength);
            var isOdd = recordSizeBytes % 2 == 1;
            if (isOdd)
            {
                // If the record size is odd, we need to add an extra byte to make it even
                // If the length is odd, an extra byte is placed after it so that the following member (optional Dx) is aligned on a 16-bit boundary.
                recordSizeBytes++;
            }

            base.RecordSizeBytes = recordSizeBytes;

            base.Write(writer);

            writer.Write(this.Y);
            writer.Write(this.X);
            writer.Write(this.StringLength > 0 ? this.StringLength : (short) textByteArray.Length);
            writer.Write((ushort) this.FwOpts);

            if (Rectangle is not null)
            {
                this.Rectangle.Write(writer);
            }

            writer.Write(textByteArray); //String (variable)

            //Dx is optional
            if (dxLength > 0)
            {
                if (isOdd)
                {
                    writer.Write(0x00);
                }
                Debug.Assert(Dx != null);

                foreach (var dx in Dx)
                {
                    writer.Write(dx);
                }
            }
        }

        protected override void Dump(StringBuilder builder)
        {
            base.Dump(builder);
            builder.AppendLine("Y: " + this.Y);
            builder.AppendLine("X: " + this.X);
            builder.AppendLine("StringLength: " + this.StringLength);
            builder.AppendLine("fwOpts: " + this.FwOpts);

            builder.AppendLine("Rectangle: ");
            this.Rectangle?.Dump(builder);

            builder.AppendLine("TextByteArray: " + string.Join(',', TextByteArray.Select(t => t.ToString("X2"))));
            builder.AppendLine("GuessText:" + WmfHelper.GetAnsiEncoding().GetString(TextByteArray));

            builder.Append("Dx:");
            if (Dx is null)
            {
                builder.AppendLine("null");
            }
            else
            {
                builder.AppendLine(string.Join(',', Dx.Select(d => d.ToString("X4"))));
            }
        }
    }
}
