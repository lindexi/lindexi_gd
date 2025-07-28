using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_EXTFLOODFILL, Size = 8)]
	public class WmfExtFloodFillRecord : WmfBinaryRecord
	{
		public WmfExtFloodFillRecord() : base()
		{
			this.ColorRef = new ColorRef();
		}

		public FloodFill Mode
		{
			get;
			set;
		}

		public ColorRef ColorRef
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
			this.Mode = (FloodFill)reader.ReadUInt16();
			this.ColorRef = reader.ReadWmfObject<ColorRef>();
			this.Y = reader.ReadInt16();
			this.X = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((ushort)this.Mode);
			writer.Write(this.ColorRef);
			writer.Write(this.Y);
			writer.Write(this.X);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Mode: " + this.Mode);
			builder.AppendLine("ColorRef: " + this.ColorRef.Dump());
			builder.AppendLine("Y: " + this.Y);
			builder.AppendLine("X: " + this.X);
		}
	}
}
