using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_CREATEPENINDIRECT, Size = 8)]
	public class WmfCreatePenIndirectRecord : WmfBinaryRecord
	{
		public WmfCreatePenIndirectRecord() : base()
		{
			this.Color = Color.Black;
			this.Style = PenStyle.PS_SOLID;
			this.Width = new Point(4, 4);
		}

		public PenStyle Style
		{
			get;
			set;
		}

		public Point Width
		{
			get;
			set;
		}

		public Color Color
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Style = (PenStyle)reader.ReadUInt16();

			short x = reader.ReadInt16();
			short y = reader.ReadInt16();
			this.Width = new Point(x, y);

			byte[] buffer = reader.ReadBytes(4);
			this.Color = Color.FromArgb(buffer[0], buffer[1], buffer[2]);
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((ushort)this.Style);
			writer.Write((ushort)this.Width.X); //PointS.X is used as width
			writer.Write((ushort)this.Width.Y); //PointS.Y is ignored according to the documentation
			writer.Write((byte)this.Color.R);
			writer.Write((byte)this.Color.G);
			writer.Write((byte)this.Color.B);
			writer.Write((byte)0x00);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("PenStyle: " + this.Style);
			builder.AppendLine("Width: " + this.Width);
			builder.AppendLine("ColorRef: " + this.Color);
		}
	}
}
