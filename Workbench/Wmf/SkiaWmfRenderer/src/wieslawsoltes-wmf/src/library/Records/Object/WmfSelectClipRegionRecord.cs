using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SELECTCLIPREGION, Size = 4)]
	public class WmfSelectClipRegionRecord : WmfBinaryRecord
	{
		public WmfSelectClipRegionRecord() : base()
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
