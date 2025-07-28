using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 40)]
	public class BitmapInfoHeader : WmfBinaryObject, IDIBHeaderInfo
	{
		public BitmapInfoHeader() : base()
		{
			this.HeaderSize = 40;
		}

		/// <summary>
		/// Gets or sets the size of this object in bytes.
		/// </summary>
		public uint HeaderSize
		{
			get;
			set;
		}

		public int Width
		{
			get;
			set;
		}

		public int Height
		{
			get;
			set;
		}

		public ushort Planes
		{
			get
			{
				return 0x0001; //Must be 0x0001 according to the documentation;
			}
		}

		public BitCount BitCount
		{
			get;
			set;
		}

		public Compression Compression
		{
			get;
			set;
		}

		public uint ImageSize
		{
			get;
			set;
		}

		public int XPelsPerMeter
		{
			get;
			set;
		}

		public int YPelsPerMeter
		{
			get;
			set;
		}

		public uint ColorUsed
		{
			get;
			set;
		}

		public uint ColorImportant
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			Read(reader, true);
		}

		public void Read(BinaryReader reader, bool readHeaderSize)
		{
			if (readHeaderSize)
			{
				this.HeaderSize = reader.ReadUInt32();
				//NOTE: Documentation does not clearly defined whether the object size is fixed (12 bytes) or variable
				//if (this.HeaderSize != 12)
				//{
				//  throw new WmfException("HeaderSize field in BitmapCoreHeader Object must be 12!");
				//}
			}

			this.Width = reader.ReadInt32();
			this.Height = reader.ReadInt32();

			ushort planes = reader.ReadUInt16();
			if (planes != this.Planes)
			{
				throw new WmfException("Planes field in BitmapInfoHeader Object must be 0x0001!");
			}

			this.BitCount = (BitCount)reader.ReadUInt16();
			this.Compression = (Compression)reader.ReadUInt32();
			this.ImageSize = reader.ReadUInt32();
			this.XPelsPerMeter = reader.ReadInt32();
			this.YPelsPerMeter = reader.ReadInt32();
			this.ColorUsed = reader.ReadUInt32();
			this.ColorImportant = reader.ReadUInt32();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.HeaderSize);
			writer.Write(this.Width);
			writer.Write(this.Height);
			writer.Write(this.Planes);
			writer.Write((ushort)this.BitCount);
			writer.Write((uint)this.Compression);
			writer.Write(this.ImageSize);
			writer.Write(this.XPelsPerMeter);
			writer.Write(this.YPelsPerMeter);
			writer.Write(this.ColorUsed);
			writer.Write(this.ColorImportant);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tHeaderSize: " + this.HeaderSize);
			builder.AppendLine("\tWidth: " + this.Width);
			builder.AppendLine("\tHeight: " + this.Height);
			builder.AppendLine("\tPlanes: " + this.Planes);
			builder.AppendLine("\tBitCount: " + this.BitCount);
			builder.AppendLine("\tCompression: " + this.Compression);
			builder.AppendLine("\tImageSize: " + this.ImageSize);
			builder.AppendLine("\tXPelsPerMeter: " + this.XPelsPerMeter);
			builder.AppendLine("\tYPelsPerMeter: " + this.YPelsPerMeter);
			builder.AppendLine("\tColorUsed: " + this.ColorUsed);
			builder.AppendLine("\tColorImportant: " + this.ColorImportant);
		}
	}
}
