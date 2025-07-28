using System;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(SizeIsVariable = true)]
	public class DeviceIndependentBitmap : WmfBinaryObject
	{
		public DeviceIndependentBitmap() : base()
		{
		}

		/// <summary>
		/// Gets or sets DIBHeaderInfo, either BitmapCoreHeader or BitmapInfoHeader.
		/// </summary>
		public IDIBHeaderInfo DIBHeaderInfo
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets an array of colors, either array of RGBQuad objects or array of uint16 values.
		/// </summary>
		public byte[] Colors
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets actual bitmap bits.
		/// </summary>
		public byte[] Data
		{
			get;
			set;
		}

		public override int GetSize()
		{
			return this.DIBHeaderInfo.GetSize() + (this.Colors != null ? this.Colors.Length : 0) + (this.Data != null ? this.Data.Length : 0);
		}

		public override void Read(BinaryReader reader)
		{
			//Determine whether to read BitmapCoreHeader or BitmapInfoHeader
			uint headerSize = reader.ReadUInt32();
			if (headerSize == 12)
			{
				var header = new BitmapCoreHeader();
				header.Read(reader, false);
				this.DIBHeaderInfo = header;
			}
			else
			{
				var header = new BitmapInfoHeader();
				header.Read(reader, false);
				this.DIBHeaderInfo = header;
			}

			//Read Colors field
			//this.Colors = reader.ReadBytes(); //TODO: How to determine length of array?!

			//Read aData field
			if (this.DIBHeaderInfo is BitmapCoreHeader)
			{
				var header = this.DIBHeaderInfo as BitmapCoreHeader;
				int length = (((header.Width * header.Planes * (int)header.BitCount + 31) & ~31) / 8) * Math.Abs(header.Height);
				this.Data = reader.ReadBytes(length);
			}
			else if (this.DIBHeaderInfo is BitmapInfoHeader)
			{
				var header = this.DIBHeaderInfo as BitmapInfoHeader;
				var colors = (int)header.ColorUsed;
				if (colors == 0) colors = 1 << (int)header.BitCount;
				Colors = reader.ReadBytes(colors * 4);

				switch (header.Compression)
				{
					case Compression.BI_RGB:
					case Compression.BI_BITFIELDS:
					case Compression.BI_CMYK:
						int length = (((header.Width * header.Planes * (int)header.BitCount + 31) & ~31) / 8) * Math.Abs(header.Height);
						this.Data = reader.ReadBytes(length);
						break;

					default:
						this.Data = reader.ReadBytes((int)header.ImageSize);
						break;
				}
			}
		}

		public override void Write(BinaryWriter writer)
		{
			this.DIBHeaderInfo.Write(writer);

			if (this.Colors != null)
			{
				writer.Write(this.Colors);
			}

			writer.Write(this.Data);

			//Write dummy byte to align padding to WORD
			if (this.Data.Length % 2 == 1)
			{
				writer.Write((byte)0x00);
			}
		}

		public override void Dump(StringBuilder builder)
		{
			builder.AppendLine("\tDIBHeaderInfo: " + this.DIBHeaderInfo.Dump());
			builder.AppendLine("\tColors: " + "?"); //TODO: Value output?
			builder.AppendLine("\taData: " + WmfHelper.DumpByteArray(this.Data));
		}
	}
}
