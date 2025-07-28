using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETMAPMODE, Size = 4)]
	public class WmfSetMapModeRecord : WmfBinaryRecord
	{
		public WmfSetMapModeRecord() : base()
		{
			//Most compatible mode (WMF Specifications, Structure Examples on page 192)
			this.Mode = MapMode.MM_ANISOTROPIC;
		}

		public MapMode Mode
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Mode = (MapMode)reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((ushort)this.Mode);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendFormat("MapMode: 0x{0:x4} (MapMode.{1})", (ushort)this.Mode, this.Mode.ToString()).AppendLine();
		}
	}
}
