using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETTEXTJUSTIFICATION, Size = 5)]
	public class WmfSetTextJustificationRecord : WmfBinaryRecord
	{
		public WmfSetTextJustificationRecord() : base()
		{
		}

		public ushort BreakCount
		{
			get;
			set;
		}

		public ushort BreakExtra
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.BreakCount = reader.ReadUInt16();
			this.BreakExtra = reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.BreakCount);
			writer.Write(this.BreakExtra);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("BreakCount: " + this.BreakCount);
			builder.AppendLine("BreakExtra: " + this.BreakExtra);
		}
	}
}
