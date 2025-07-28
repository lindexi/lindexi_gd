using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 36)] 
	public class CIEXYZTriple : WmfBinaryObject
	{
		public CIEXYZTriple() : base()
		{
			this.Red = new CIEXYZ();
			this.Green = new CIEXYZ();
			this.Blue = new CIEXYZ();
		}

		public CIEXYZ Red
		{
			get;
			set;
		}

		public CIEXYZ Green
		{
			get;
			set;
		}

		public CIEXYZ Blue
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Red = reader.ReadWmfObject<CIEXYZ>();
			this.Green = reader.ReadWmfObject<CIEXYZ>();
			this.Blue = reader.ReadWmfObject<CIEXYZ>();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Red);
			writer.Write(this.Green);
			writer.Write(this.Blue);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);

			builder.AppendLine("\tciexyzRed: ");
			this.Red.Dump(builder);

			builder.AppendLine("\tciexyzGreen: ");
			this.Green.Dump(builder);

			builder.AppendLine("\tciexyzBlue: ");
			this.Blue.Dump(builder);
		}
	}
}
