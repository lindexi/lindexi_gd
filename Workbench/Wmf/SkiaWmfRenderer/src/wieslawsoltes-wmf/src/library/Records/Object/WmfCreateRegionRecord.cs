using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_CREATEREGION, SizeIsVariable = true)]
	public class WmfCreateRegionRecord : WmfBinaryRecord
	{
		public WmfCreateRegionRecord() : base()
		{
		}

		public Region Region
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Region = new Region();
			this.Region.Read(reader);
		}

		public override void Write(BinaryWriter writer)
		{
			base.RecordSizeBytes = (uint)(6 /* Record header */ + this.Region.GetSize());
			base.Write(writer);
			writer.Write(this.Region);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Region: ");
			this.Region.Dump(builder);
		}
	}
}
