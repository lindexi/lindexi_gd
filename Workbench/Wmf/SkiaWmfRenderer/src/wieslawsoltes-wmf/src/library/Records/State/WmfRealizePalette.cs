using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_REALIZEPALETTE, Size = 3)]
	public class WmfRealizePalette : WmfBinaryRecord
	{
		public WmfRealizePalette() : base()
		{
		}

		public override void Read(BinaryReader reader)
		{

		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
		}
	}
}
