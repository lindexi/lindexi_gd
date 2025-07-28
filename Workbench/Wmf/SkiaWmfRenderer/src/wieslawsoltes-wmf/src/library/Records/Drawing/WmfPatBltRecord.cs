using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_PATBLT, Size = 9)]
	public class WmfPatBltRecord : WmfBinaryRecord
	{
		public WmfPatBltRecord() : base()
		{
		}

		public TernaryRasterOperation RasterOperation
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

		public short YLeft
		{
			get;
			set;
		}

		public short XLeft
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.RasterOperation = (TernaryRasterOperation)reader.ReadUInt32();
			this.Height = reader.ReadInt16();
			this.Width = reader.ReadInt16();
			this.YLeft = reader.ReadInt16();
			this.XLeft = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((uint)this.RasterOperation);
			writer.Write(this.Height);
			writer.Write(this.Width);
			writer.Write(this.YLeft);
			writer.Write(this.XLeft);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("RasterOperation: " + this.RasterOperation);
			builder.AppendLine("Height: " + this.Height);
			builder.AppendLine("Width: " + this.Width);
			builder.AppendLine("YLeft: " + this.YLeft);
			builder.AppendLine("XLeft: " + this.XLeft);
		}
	}
}
