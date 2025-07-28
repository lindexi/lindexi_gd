using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 8)]
	public class LogBrush : WmfBinaryObject
	{
		public LogBrush() : base()
		{
			this.Color = Color.Black;
			this.Style = BrushStyle.BS_SOLID;
			this.Hatch = HatchStyle.HS_HORIZONTAL;
		}

		public BrushStyle Style
		{
			get;
			set;
		}

		public Color Color
		{
			get;
			set;
		}

		public HatchStyle Hatch
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Style = (BrushStyle)reader.ReadUInt16();

			byte[] buffer = reader.ReadBytes(4);
			this.Color = Color.FromArgb(buffer[0], buffer[1], buffer[2]);

			this.Hatch = (HatchStyle)reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write((ushort)this.Style);
			writer.Write((byte)this.Color.R);
			writer.Write((byte)this.Color.G);
			writer.Write((byte)this.Color.B);
			writer.Write((byte)0x00);
			writer.Write((ushort)this.Hatch);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tBrushStyle: " + this.Style);
			builder.AppendLine("\tColorRef: " + this.Color);
			builder.AppendLine("\tBrushHatch: " + this.Hatch);
		}
	}
}
