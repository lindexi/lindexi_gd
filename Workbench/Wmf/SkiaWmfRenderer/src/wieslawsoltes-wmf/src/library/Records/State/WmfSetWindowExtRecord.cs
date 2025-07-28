using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETWINDOWEXT, Size = 5)]
	public class WmfSetWindowExtRecord : WmfBinaryRecord
	{
		public WmfSetWindowExtRecord() : base()
		{
		}

		public short Y
		{
			get;
			set;
		}

		public short X
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Y = reader.ReadInt16();
			this.X = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.Y); //Note that Y is before X
			writer.Write(this.X);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Y: " + this.Y);
			builder.AppendLine("X: " + this.X);
		}
	}
}
