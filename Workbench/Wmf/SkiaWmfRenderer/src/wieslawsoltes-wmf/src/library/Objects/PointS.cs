using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 4)]
	public class PointS : WmfBinaryObject
	{
		public PointS() : base()
		{
		}

		public short X
		{
			get;
			set;
		}

		public short Y
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.X = reader.ReadInt16();
			this.Y = reader.ReadInt16();
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
