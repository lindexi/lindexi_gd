using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 8)]
	public class PointL : WmfBinaryObject
	{
		public PointL() : base()
		{
		}

		public int X
		{
			get;
			set;
		}

		public int Y
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.X = reader.ReadInt32();
			this.Y = reader.ReadInt32();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.X);
			writer.Write(this.Y);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tx: " + this.X);
			builder.AppendLine("\ty: " + this.Y);
		}
	}
}
