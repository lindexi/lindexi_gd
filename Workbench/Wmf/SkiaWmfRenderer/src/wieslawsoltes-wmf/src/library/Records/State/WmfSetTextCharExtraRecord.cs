using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETTEXTCHAREXTRA, Size = 4)]
	public class WmfSetTextCharExtraRecord : WmfBinaryRecord
	{
		public WmfSetTextCharExtraRecord() : base()
		{
		}

		public ushort CharExtra
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.CharExtra = reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.CharExtra);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("CharExtra: " + this.CharExtra);
		}
	}
}
