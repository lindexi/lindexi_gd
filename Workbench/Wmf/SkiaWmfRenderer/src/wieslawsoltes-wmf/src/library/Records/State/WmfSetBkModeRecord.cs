using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETBKMODE, Size = 4)]
	public class WmfSetBkModeRecord : WmfBinaryRecord
	{
		public WmfSetBkModeRecord() : base()
		{
		}

		public MixMode Mode
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Mode = (MixMode)reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((ushort)this.Mode);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendFormat("MixMode: 0x{0:x4} (MixMode.{1})", (ushort)this.Mode, this.Mode.ToString()).AppendLine();
		}
	}
}
