using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_FILLREGION, Size = 5)]
	public class WmfFillRegionRecord : WmfBinaryRecord
	{
		public WmfFillRegionRecord() : base()
		{
		}

		public ushort Region
		{
			get;
			set;
		}

		public ushort Brush
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Region = reader.ReadUInt16();
			this.Brush = reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.Region);
			writer.Write(this.Brush);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Region: " + this.Region);
			builder.AppendLine("Brush: " + this.Brush);
		}
	}
}
