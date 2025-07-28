using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_INVERTREGION, Size = 4)]
	public class WmfInvertRegionRecord : WmfBinaryRecord
	{
		public WmfInvertRegionRecord() : base()
		{
		}

		public ushort Region
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Region = reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.Region);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Region: " + this.Region);
		}
	}
}
