using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 10)]
	public class Pen : WmfBinaryObject
	{
		public Pen() : base()
		{
		}

		public PenStyle PenStyle
		{
			get;
			set;
		}

		public PointS Width
		{
			get;
			set;
		}

		public ColorRef ColorRef
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.PenStyle = (PenStyle)reader.ReadUInt16();
			this.Width = reader.ReadWmfObject<PointS>();
			this.ColorRef = reader.ReadWmfObject<ColorRef>();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write((ushort)this.PenStyle);
			writer.Write(this.Width);
			writer.Write(this.ColorRef);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tPenStyle: " + this.PenStyle);

			builder.AppendLine("\tWidth: ");
			this.Width.Dump(builder);

			builder.AppendLine("\tColorRef: ");
			this.ColorRef.Dump(builder);
		}
	}
}
