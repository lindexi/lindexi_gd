using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 12)] 
	public class CIEXYZ : WmfBinaryObject
	{
		public CIEXYZ() : base()
		{
		}

		public float X
		{
			get;
			set;
		}

		public float Y
		{
			get;
			set;
		}

		public float Z
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.X = reader.ReadSingle();
			this.Y = reader.ReadSingle();
			this.Z = reader.ReadSingle();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.X);
			writer.Write(this.Y);
			writer.Write(this.Z);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tciexyzX: " + this.X);
			builder.AppendLine("\tciexyzY: " + this.Y);
			builder.AppendLine("\tciexyzZ: " + this.Z);
		}
	}
}
