using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_DIBCREATEPATTERNBRUSH, SizeIsVariable = true)]
	public class WmfDIBCreatePatternBrushRecord : WmfBinaryRecord
	{
		public WmfDIBCreatePatternBrushRecord() : base()
		{
		}

		public BrushStyle Style
		{
			get;
			set;
		}

		public ColorUsage ColorUsage
		{
			get;
			set;
		}

		public DeviceIndependentBitmap Target
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Style = (BrushStyle)reader.ReadUInt16();
			this.ColorUsage = (ColorUsage)reader.ReadUInt16();
			this.Target = reader.ReadWmfObject<DeviceIndependentBitmap>(); //TODO: How many bytes?
		}

		public override void Write(BinaryWriter writer)
		{
			base.RecordSizeBytes = (uint)(10 /* Fixed size fields */ + this.Target.GetSize());
			base.Write(writer);
			writer.Write((ushort)this.Style);
			writer.Write((ushort)this.ColorUsage);
			writer.Write(this.Target);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Style: " + this.Style);
			builder.AppendLine("ColorUsage: " + this.ColorUsage);
			builder.AppendLine("Target: " + this.Target.Dump());
		}
	}
}
