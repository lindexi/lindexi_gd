using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 8)]
	public class SizeL : WmfBinaryObject
	{
		public SizeL() : base()
		{
		}

		public uint CX
		{
			get;
			set;
		}

		public uint CY
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.CX = reader.ReadUInt32();
			this.CY = reader.ReadUInt32();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.CX);
			writer.Write(this.CY);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tcx: " + this.CX);
			builder.AppendLine("\tcy: " + this.CY);
		}
	}
}
