using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETTEXTCOLOR, Size = 5)]
	public class WmfSetTextColorRecord : WmfBinaryRecord
	{
		public WmfSetTextColorRecord() : base()
		{
		}

		public Color Color
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			//ColorRef
			byte[] buffer = reader.ReadBytes(4);
			this.Color = Color.FromArgb(buffer[0], buffer[1], buffer[2]);
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);

			//ColorRef
			writer.Write((byte)this.Color.R);
			writer.Write((byte)this.Color.G);
			writer.Write((byte)this.Color.B);
			writer.Write((byte)0x00);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("ColorRef: " + this.Color);
		}
	}
}
