using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 12)]
	public class BitmapCoreHeader : WmfBinaryObject, IDIBHeaderInfo
	{
		public BitmapCoreHeader() : base()
		{
			this.HeaderSize = 12;
		}

		/// <summary>
		/// Gets or sets the size of this object in bytes.
		/// </summary>
		public uint HeaderSize
		{
			get;
			set;
		}

		public ushort Width
		{
			get;
			set;
		}

		public ushort Height
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

			this.Width = reader.ReadUInt16();
			this.Height = reader.ReadUInt16();

			ushort planes = reader.ReadUInt16();
			if (planes != this.Planes)
			{
				throw new WmfException("Planes field in BitmapCoreHeader Object must be 0x0001!");
			}

			this.BitCount = (BitCount)reader.ReadUInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.HeaderSize);
			writer.Write(this.Width);
			writer.Write(this.Height);
			writer.Write(this.Planes);
			writer.Write((ushort)this.BitCount);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tHeaderSize: " + this.HeaderSize);
			builder.AppendLine("\tWidth: " + this.Width);
			builder.AppendLine("\tHeight: " + this.Height);
			builder.AppendLine("\tPlanes: " + this.Planes);
			builder.AppendLine("\tBitCount: " + this.BitCount);
		}
	}
}
