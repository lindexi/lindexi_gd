using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SELECTOBJECT, Size = 4)]
	public class WmfSelectObjectRecord : WmfBinaryRecord
	{
		public WmfSelectObjectRecord() : base()
		{
		}

		public ushort ObjectIndex
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.ObjectIndex = reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.ObjectIndex);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("ObjectIndex: " + this.ObjectIndex);
		}
	}
}
