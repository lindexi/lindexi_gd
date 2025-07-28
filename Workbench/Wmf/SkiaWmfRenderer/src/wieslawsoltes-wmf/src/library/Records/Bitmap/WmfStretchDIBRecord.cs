using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_STRETCHDIB, SizeIsVariable = true)]
	public class WmfStretchDIBRecord : WmfBinaryRecord
	{
		public WmfStretchDIBRecord() : base()
		{
		}

		public TernaryRasterOperation RasterOperation
		{
			get;
			set;
		}

		public ColorUsage ColorUsage
		{
			get;
			set;
		}

		public short SrcHeight
		{
			get;
			set;
		}

		public short SrcWidth
		{
			get;
			set;
		}

		public short YSrc
		{
			get;
			set;
		}

		public short XSrc
		{
			get;
			set;
		}

		public short DestHeight
		{
			get;
			set;
		}

		public short DestWidth
		{
			get;
			set;
		}

		public short YDest
		{
			get;
			set;
		}

		public short XDest
		{
			get;
			set;
		}

		public DeviceIndependentBitmap DIB
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			bool isWithoutBitmap = (this.RecordSize == 24);

			this.RasterOperation = (TernaryRasterOperation)reader.ReadUInt32();
			this.ColorUsage = (ColorUsage)reader.ReadUInt16();

			if (!isWithoutBitmap)
			{
				this.SrcHeight = reader.ReadInt16();
				this.SrcWidth = reader.ReadInt16();
			}

			this.YSrc = reader.ReadInt16();
			this.XSrc = reader.ReadInt16();

			if (isWithoutBitmap)
			{
				short dummy = reader.ReadInt16();
			}

			this.DestHeight = reader.ReadInt16();
			this.DestWidth = reader.ReadInt16();
			this.YDest = reader.ReadInt16();
			this.XDest = reader.ReadInt16();

			if (!isWithoutBitmap)
			{
				this.DIB = reader.ReadWmfObject<DeviceIndependentBitmap>();
			}
		}

		public override void Write(BinaryWriter writer)
		{
			base.RecordSizeBytes = (uint)(6 /* RecordSize and RecordFunction */ + 22 /* Fixed size fields */ + (this.DIB == null ? 0 : this.DIB.GetSize()));
			base.Write(writer);

			writer.Write((uint)this.RasterOperation);
			writer.Write((ushort)this.ColorUsage);
			writer.Write(this.SrcHeight);
			writer.Write(this.SrcWidth);
			writer.Write(this.YSrc);
			writer.Write(this.XSrc);
			writer.Write(this.DestHeight);
			writer.Write(this.DestWidth);
			writer.Write(this.YDest);
			writer.Write(this.XDest);
			writer.Write(this.DIB);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("RasterOperation: " + this.RasterOperation);
			builder.AppendLine("ColorUsage: " + this.ColorUsage);
			builder.AppendLine("SrcHeight: " + this.SrcHeight);
			builder.AppendLine("SrcWidth: " + this.SrcWidth);
			builder.AppendLine("YSrc: " + this.YSrc);
			builder.AppendLine("XSrc: " + this.XSrc);
			builder.AppendLine("DestHeight: " + this.DestHeight);
			builder.AppendLine("DestWidth: " + this.DestWidth);
			builder.AppendLine("YDest: " + this.YDest);
			builder.AppendLine("XDest: " + this.XDest);

			builder.AppendLine("DIB: ");
			this.DIB.Dump(builder);
		}
	}
}
