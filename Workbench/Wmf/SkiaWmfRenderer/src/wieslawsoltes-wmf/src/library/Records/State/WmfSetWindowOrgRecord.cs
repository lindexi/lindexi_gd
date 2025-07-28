using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETWINDOWORG, Size = 5)]
	public class WmfSetWindowOrgRecord : WmfBinaryRecord
	{
		public WmfSetWindowOrgRecord() : base()
		{
		}

		public short X
		{
			get;
			set;
		}

		public short Y
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.X = reader.ReadInt16();
			this.Y = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.X);
			writer.Write(this.Y);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("X: " + this.X);
			builder.AppendLine("Y: " + this.Y);
		}
	}
}
