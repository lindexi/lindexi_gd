using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETPIXEL, Size = 7)]
	public class WmfSetPixelRecord : WmfBinaryRecord
	{
		public WmfSetPixelRecord() : base()
		{
		}

		public Color Color
		{
			get;
			set;
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

		public override void Read(BinaryReader reader)
		{
			//ColorRef
			byte[] buffer = reader.ReadBytes(4);
			this.Color = Color.FromArgb(buffer[0], buffer[1], buffer[2]);

			this.Y = reader.ReadInt16();
			this.X = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);

			//ColorRef
			writer.Write((byte)this.Color.R);
			writer.Write((byte)this.Color.G);
			writer.Write((byte)this.Color.B);
			writer.Write((byte)0x00);

			writer.Write(this.Y);
			writer.Write(this.X);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("ColorRef: " + this.Color);
			builder.AppendLine("Y: " + this.Y);
			builder.AppendLine("X: " + this.X);
		}
	}
}
