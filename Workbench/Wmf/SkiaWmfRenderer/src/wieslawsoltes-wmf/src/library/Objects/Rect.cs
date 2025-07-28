using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 8)]
	public class Rect : WmfBinaryObject
	{
		public Rect() : base()
		{
		}

		public short Left
		{
			get;
			set;
		}

		public short Top
		{
			get;
			set;
		}

		public short Right
		{
			get;
			set;
		}

		public short Bottom
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Left = reader.ReadInt16();
			this.Top = reader.ReadInt16();
			this.Right = reader.ReadInt16();
			this.Bottom = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Left);
			writer.Write(this.Top);
			writer.Write(this.Right);
			writer.Write(this.Bottom);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tLeft: " + this.Left);
			builder.AppendLine("\tTop: " + this.Top);
			builder.AppendLine("\tRight: " + this.Right);
			builder.AppendLine("\tBottom: " + this.Bottom);
		}
	}
}
