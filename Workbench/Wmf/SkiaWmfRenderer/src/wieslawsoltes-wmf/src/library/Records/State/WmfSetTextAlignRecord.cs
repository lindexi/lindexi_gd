using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETTEXTALIGN /* Size = 4 */)] //Fixed size by spec. but some WMF files have variable size
	public class WmfSetTextAlignRecord : WmfBinaryRecord
	{
		public WmfSetTextAlignRecord() : base()
		{
		}

		public TextAlignmentMode Mode
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Mode = (TextAlignmentMode)reader.ReadUInt16();

			//Work-around for some WMF files
			if (base.RecordSizeBytes > 8)
			{
				//Skip unknown bytes
				reader.BaseStream.Seek(base.RecordSizeBytes - 8, SeekOrigin.Current);
			}
		}

		public override void Write(BinaryWriter writer)
		{
			base.RecordSize = 4;
			base.Write(writer);
			writer.Write((ushort)this.Mode);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendFormat("TextAlignmentMode: 0x{0:x4} (TextAlignmentMode.{1})", (ushort)this.Mode, this.Mode.ToString()).AppendLine();
		}
	}
}
