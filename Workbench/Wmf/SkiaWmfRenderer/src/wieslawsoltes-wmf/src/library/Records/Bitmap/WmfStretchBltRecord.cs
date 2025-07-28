using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_STRETCHBLT, SizeIsVariable = true)]
	public class WmfStretchBltRecord : WmfBinaryRecord
	{
		public WmfStretchBltRecord() : base()
		{
		}

		public TernaryRasterOperation RasterOperation
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

		public Bitmap16 Target
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			bool isWithoutBitmap = (this.RecordSize == 24);

			this.RasterOperation = (TernaryRasterOperation)reader.ReadUInt32();

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
				this.Target = reader.ReadWmfObject<Bitmap16>();
			}
		}

		public override void Write(BinaryWriter writer)
		{
			base.RecordSizeBytes = (uint)(6 /* RecordSize and RecordFunction */ + 18 /* Fixed size fields */ + (this.Target == null ? 2 /* Reserved length */ : this.Target.GetSize()));
			base.Write(writer);

			writer.Write((uint)this.RasterOperation);

			if (this.Target != null)
			{
				writer.Write(this.SrcHeight);
				writer.Write(this.SrcWidth);
			}

			writer.Write(this.YSrc);
			writer.Write(this.XSrc);

			if (this.Target == null)
			{
				writer.Write((short)0x0000);
			}

			writer.Write(this.DestHeight);
			writer.Write(this.DestWidth);
			writer.Write(this.YDest);
			writer.Write(this.XDest);

			if (this.Target != null)
			{
				writer.Write(this.Target);
			}
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("RasterOperation: " + this.RasterOperation);

			if (this.Target != null)
			{
				builder.AppendLine("SrcHeight: " + this.SrcHeight);
				builder.AppendLine("SrcWidth: " + this.SrcWidth);
			}

			builder.AppendLine("YSrc: " + this.YSrc);
			builder.AppendLine("XSrc: " + this.XSrc);
			builder.AppendLine("DestHeight: " + this.DestHeight);
			builder.AppendLine("DestWidth: " + this.DestWidth);
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
