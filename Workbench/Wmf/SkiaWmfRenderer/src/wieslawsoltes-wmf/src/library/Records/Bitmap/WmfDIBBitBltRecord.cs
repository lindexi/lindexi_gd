using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_DIBBITBLT, SizeIsVariable = true)]
	public class WmfDIBBitBltRecord : WmfBinaryRecord
	{
		public WmfDIBBitBltRecord() : base()
		{
		}

		public TernaryRasterOperation RasterOperation
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

		public DeviceIndependentBitmap Target
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			bool isWithoutBitmap = (this.RecordSize == 24);

			this.RasterOperation = (TernaryRasterOperation)reader.ReadUInt32();
			this.YSrc = reader.ReadInt16();
			this.XSrc = reader.ReadInt16();

			//Reserved, see 2.3.1.1.2 Without Bitmap
			if (isWithoutBitmap)
			{
				short dummy = reader.ReadInt16();
			}

			this.Height = reader.ReadInt16();
			this.Width = reader.ReadInt16();
			this.YDest = reader.ReadInt16();
			this.XDest = reader.ReadInt16();

			if (!isWithoutBitmap)
			{
				this.Target = reader.ReadWmfObject<DeviceIndependentBitmap>();
			}
		}

		public override void Write(BinaryWriter writer)
		{
			base.RecordSizeBytes = (uint)(6 /* RecordSize and RecordFunction */ + 18 /* Fixed size fields */ + (this.Target == null ? 2 /* Reserved length */ : this.Target.GetSize()));
			base.Write(writer);

			writer.Write((uint)this.RasterOperation);
			writer.Write(this.YSrc);
			writer.Write(this.XSrc);

			//Reserved, see 2.3.1.1.2 Without Bitmap
			if (this.Target == null)
			{
				writer.Write((short)0x0000);
			}

			writer.Write(this.Height);
			writer.Write(this.Width);
			writer.Write(this.YDest);
			writer.Write(this.XDest);

			if (this.Target != null)
			{
				this.Target.Write(writer);
			}
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("RasterOperation: " + this.RasterOperation);
			builder.AppendLine("YSrc: " + this.YSrc);
			builder.AppendLine("XSrc: " + this.XSrc);
			builder.AppendLine("Height: " + this.Height);
			builder.AppendLine("Width: " + this.Width);
			builder.AppendLine("YDest: " + this.YDest);
			builder.AppendLine("XDest: " + this.XDest);

			if (this.Target != null)
			{
				builder.AppendLine("Target: ");
				this.Target.Dump(builder);
			}
			else
			{
				builder.AppendLine("Target: (without bitmap)");
			}
		}
	}
}
