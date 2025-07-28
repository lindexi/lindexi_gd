using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_SETDIBTODEV, SizeIsVariable = true)]
	public class WmfSetDIBToDevRecord : WmfBinaryRecord
	{
		public WmfSetDIBToDevRecord() : base()
		{
		}
		
		public ColorUsage ColorUsage
		{
			get;
			set;
		}

		public ushort ScanCount
		{
			get;
			set;
		}

		public ushort StartScan
		{
			get;
			set;
		}

		public ushort YDIB
		{
			get;
			set;
		}

		public ushort XDIB
		{
			get;
			set;
		}

		public ushort Height
		{
			get;
			set;
		}

		public ushort Width
		{
			get;
			set;
		}

		public ushort YDest
		{
			get;
			set;
		}

		public ushort XDest
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
			this.ColorUsage = (ColorUsage)reader.ReadUInt16();
			this.ScanCount = reader.ReadUInt16();
			this.StartScan = reader.ReadUInt16();
			this.YDIB = reader.ReadUInt16();
			this.XDIB = reader.ReadUInt16();
			this.Height = reader.ReadUInt16();
			this.Width = reader.ReadUInt16();
			this.YDest = reader.ReadUInt16();
			this.XDest = reader.ReadUInt16();
			this.DIB = reader.ReadWmfObject<DeviceIndependentBitmap>();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write((ushort)this.ColorUsage);
			writer.Write(this.ScanCount);
			writer.Write(this.StartScan);
			writer.Write(this.YDIB);
			writer.Write(this.XDIB);
			writer.Write(this.Height);
			writer.Write(this.Width);
			writer.Write(this.YDest);
			writer.Write(this.XDest);
			writer.Write(this.DIB);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("ColorUsage: " + this.ColorUsage);
			builder.AppendLine("ScanCount: " + this.ScanCount);
			builder.AppendLine("StartScan: " + this.StartScan);
			builder.AppendLine("yDib: " + this.YDIB);
			builder.AppendLine("xDib: " + this.XDIB);
			builder.AppendLine("Height: " + this.Height);
			builder.AppendLine("Width: " + this.Width);
			builder.AppendLine("yDest: " + this.YDest);
			builder.AppendLine("xDest: " + this.XDest);

			builder.AppendLine("DIB: ");
			this.DIB.Dump(builder);
		}
	}
}
