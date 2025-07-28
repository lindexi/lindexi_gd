using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 108)]
	public class BitmapV4Header : WmfBinaryObject
	{
		public BitmapV4Header() : base()
		{
			this.BitmapInfoHeader = new BitmapInfoHeader();
			this.Endpoints = new CIEXYZTriple();
		}

		public BitmapInfoHeader BitmapInfoHeader
		{
			get;
			set;
		}

		public uint RedMask
		{
			get;
			set;
		}

		public uint GreenMask
		{
			get;
			set;
		}

		public uint BlueMask
		{
			get;
			set;
		}

		public uint AlphaMask
		{
			get;
			set;
		}

		public LogicalColorSpace ColorSpaceType
		{
			get;
			set;
		}

		public CIEXYZTriple Endpoints
		{
			get;
			set;
		}

		public float GammaRed
		{
			get;
			set;
		}

		public float GammaGreen
		{
			get;
			set;
		}

		public float GammaBlue
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.BitmapInfoHeader = reader.ReadWmfObject<BitmapInfoHeader>();
			this.RedMask = reader.ReadUInt32();
			this.GreenMask = reader.ReadUInt32();
			this.BlueMask = reader.ReadUInt32();
			this.AlphaMask = reader.ReadUInt32();
			this.ColorSpaceType = (LogicalColorSpace)reader.ReadUInt32();
			this.Endpoints = reader.ReadWmfObject<CIEXYZTriple>();
			this.GammaRed = reader.ReadSingle();
			this.GammaGreen = reader.ReadSingle();
			this.GammaBlue = reader.ReadSingle();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.BitmapInfoHeader);
			writer.Write(this.RedMask);
			writer.Write(this.GreenMask);
			writer.Write(this.BlueMask);
			writer.Write(this.AlphaMask);
			writer.Write((uint)this.ColorSpaceType);
			writer.Write(this.Endpoints);
			writer.Write(this.GammaRed);
			writer.Write(this.GammaGreen);
			writer.Write(this.GammaBlue);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			this.BitmapInfoHeader.Dump(builder);
			builder.AppendLine("\tRedMask: " + this.RedMask);
			builder.AppendLine("\tGreenMask: " + this.GreenMask);
			builder.AppendLine("\tBlueMask: " + this.BlueMask);
			builder.AppendLine("\tAlphaMask: " + this.AlphaMask);
			builder.AppendLine("\tColorSpaceType: " + this.ColorSpaceType);
			this.Endpoints.Dump(builder);
			builder.AppendLine("\tGammaRed: " + this.GammaRed);
			builder.AppendLine("\tGammaGreen: " + this.GammaGreen);
			builder.AppendLine("\tGammaBlue: " + this.GammaBlue);
		}
	}
}
