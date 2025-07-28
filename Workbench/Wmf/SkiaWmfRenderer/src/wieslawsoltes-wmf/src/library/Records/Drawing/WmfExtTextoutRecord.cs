using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	//NOTE: The attribute is commented because the records is not fully implemented and thus wont be present while parsing WMF document
	//[WmfRecord(Type = RecordType.META_EXTTEXTOUT, SizeIsVariable = true)]
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

		public ExtTextOutOptions fwOpts
		{
			get;
			set;
		}

		public Rect Rectangle
		{
			get;
			set;
		}

		public string StringValue
		{
			get;
			set;
		}

		public byte[] Dx
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
			this.Y = reader.ReadInt16();
			this.X = reader.ReadInt16();
			this.StringLength = reader.ReadInt16();
			this.fwOpts = (ExtTextOutOptions)reader.ReadUInt16();

			//TODO: Rectangle is optional, what if empty?!
			this.Rectangle = new Rect();
			this.Rectangle.Read(reader);

			if (this.StringLength > 0)
			{
				byte[] ansi = reader.ReadBytes(this.StringLength);
				this.StringValue = StringEncoding.GetString(ansi);
			}

			//this.Dx = reader.ReadBytes(); //TODO: Number of bytes left
		}

		public override void Write(BinaryWriter writer)
		{
			int dxLength = (this.Dx != null ? this.Dx.Length : 0);
			byte[] ansi = StringEncoding.GetBytes(this.StringValue ?? "");

			base.RecordSizeBytes = (uint)(6 /* RecordSize and RecordFunction */ + 16 + ansi.Length + dxLength);
			base.Write(writer);

			writer.Write(this.Y);
			writer.Write(this.X);
			writer.Write(this.StringLength > 0 ? this.StringLength : (short)ansi.Length);
			writer.Write((ushort)this.fwOpts);
			this.Rectangle.Write(writer); //TODO: Rectangle is optional, what if empty?!
			writer.Write(ansi); //String (variable)

			//Dx is optional
			if (dxLength > 0)
			{
				writer.Write(this.Dx);
			}
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Y: " + this.Y);
			builder.AppendLine("X: " + this.X);
			builder.AppendLine("StringLength: " + this.StringLength);
			builder.AppendLine("fwOpts: " + this.fwOpts);

			builder.AppendLine("Rectangle: ");
			this.Rectangle.Dump(builder);

			builder.AppendLine("StringValue: " + this.StringValue);
			//builder.AppendLine("Dx: " + this.Dx);
		}
	}
}
