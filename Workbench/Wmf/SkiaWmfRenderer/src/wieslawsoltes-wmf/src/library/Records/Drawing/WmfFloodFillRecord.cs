using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_FLOODFILL, Size = 7)]
	public class WmfFloodFillRecord : WmfBinaryRecord
	{
		public WmfFloodFillRecord() : base()
		{
			this.ColorRef = new ColorRef();
		}

		public ColorRef ColorRef
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

		public override void Read(BinaryReader reader)
		{
			this.ColorRef = reader.ReadWmfObject<ColorRef>();
			this.YStart = reader.ReadInt16();
			this.XStart = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.ColorRef);
			writer.Write(this.YStart);
			writer.Write(this.XStart);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("ColorRef: " + this.ColorRef.Dump());
			builder.AppendLine("YStart: " + this.YStart);
			builder.AppendLine("XStart: " + this.XStart);
		}
	}
}
