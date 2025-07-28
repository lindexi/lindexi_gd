using System.Drawing;
using System.IO;
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

		public string StringValue
		{
			get;
			set;
		}

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
				byte[] ansi = reader.ReadBytes(this.StringLength);
				this.StringValue = StringEncoding.GetString(ansi);
			}

			this.YStart = reader.ReadInt16();
			this.XStart = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			byte[] ansi = StringEncoding.GetBytes(this.StringValue ?? "");
			int offset = (ansi.Length % 2 == 1 ? +1 : +0); //1 extra byte for odd-length string

			base.RecordSizeBytes = (uint)(6 /* RecordSize and RecordFunction */ + 2 + (ansi.Length + offset) + 2 + 2 /* Parameters */);
			base.Write(writer);
			
			writer.Write(this.StringLength > 0 ? this.StringLength : (short)(ansi.Length + offset));
			writer.Write(ansi);

			if (ansi.Length % 2 == 1)
			{
				//Write a dummy byte after odd-length string so the record aligns to 16-bit boundary
				writer.Write((byte)0x00);
			}

			writer.Write(this.YStart);
			writer.Write(this.XStart);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("StringLength: " + this.StringLength);
			builder.AppendLine("String: " + this.StringValue);
			builder.AppendLine("YStart: " + this.YStart);
			builder.AppendLine("XStart: " + this.XStart);
		}
	}
}
