using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(SizeIsVariable = true)]
	public class Bitmap16 : WmfBinaryObject
	{
		public Bitmap16() : base()
		{
		}

		public short Type
		{
			get;
			set;
		}

		public short Width
		{
			get;
			set;
		}

		public short Height
		{
			get;
			set;
		}

		public short WidthBytes
		{
			get;
			set;
		}

		public byte Planes
		{
			get
			{
				return 0x01; //Must be 0x01 according to the documentation
			}
		}

		public byte BitsPixel
		{
			get;
			set;
		}

		public byte[] Bits
		{
			get;
			set;
		}

		public override int GetSize()
		{
			return 10 /* Fixed size fields */ + (this.Bits != null ? this.Bits.Length : 0);
		}

		public override void Read(BinaryReader reader)
		{
			this.Type = reader.ReadInt16();
			this.Width = reader.ReadInt16();
			this.Height = reader.ReadInt16();
			this.WidthBytes = reader.ReadInt16();

			byte planes = reader.ReadByte();
			if (planes != this.Planes)
			{
				throw new WmfException("Planes field in Bitmap16 Object must be 0x01!");
			}

			this.BitsPixel = reader.ReadByte();

			int length = (((this.Width * this.BitsPixel + 15) >> 4) << 1) * this.Height; //Calculation according to the documentation
			this.Bits = reader.ReadBytes(length);
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Type);
			writer.Write(this.Width);
			writer.Write(this.Height);
			writer.Write(this.WidthBytes);
			writer.Write(this.Planes);
			writer.Write(this.BitsPixel);
			writer.Write(this.Bits);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tType: " + this.Type);
			builder.AppendLine("\tWidth: " + this.Width);
			builder.AppendLine("\tHeight: " + this.Height);
			builder.AppendLine("\tWidthBytes: " + this.WidthBytes);
			builder.AppendLine("\tPlanes: " + this.Planes);
			builder.AppendLine("\tBitsPixel: " + this.BitsPixel);
			builder.AppendLine("\tBits: " + WmfHelper.DumpByteArray(this.Bits));
		}
	}
}
