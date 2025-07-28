using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETPOLYFILLMODE, Size = 4)]
	public class WmfSetPolyFillModeRecord : WmfBinaryRecord
	{
		public WmfSetPolyFillModeRecord() : base()
		{
		}

		public PolyFillMode Mode
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Mode = (PolyFillMode)reader.ReadUInt16();
			reader.Skip((int)base.RecordSizeBytes - 8);
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((ushort)this.Mode);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendFormat("PolyFillMode: 0x{0:x4} (PolyFillMode.{1})", (ushort)this.Mode, this.Mode.ToString()).AppendLine();
		}
	}
}
