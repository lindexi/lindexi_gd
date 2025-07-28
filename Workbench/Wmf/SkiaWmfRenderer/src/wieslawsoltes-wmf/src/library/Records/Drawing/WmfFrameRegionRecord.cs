using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_FRAMEREGION, Size = 7)]
	public class WmfFrameRegionRecord : WmfBinaryRecord
	{
		public WmfFrameRegionRecord() : base()
		{
		}

		public ushort Region
		{
			get;
			set;
		}

		public ushort Brush
		{
			get;
			set;
		}

		public short Height
		{
			get;
			set;
		}

		public short Width
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Region = reader.ReadUInt16();
			this.Brush = reader.ReadUInt16();
			this.Height = reader.ReadInt16();
			this.Width = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.Region);
			writer.Write(this.Brush);
			writer.Write(this.Height);
			writer.Write(this.Width);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Region: " + this.Region);
			builder.AppendLine("Brush: " + this.Brush);
			builder.AppendLine("Height: " + this.Height);
			builder.AppendLine("Width: " + this.Width);
		}
	}
}
