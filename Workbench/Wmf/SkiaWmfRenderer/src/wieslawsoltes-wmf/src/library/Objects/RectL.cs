using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 16)]
	public class RectL : WmfBinaryObject
	{
		public RectL() : base()
		{
		}

		public int Left
		{
			get;
			set;
		}

		public int Top
		{
			get;
			set;
		}

		public int Right
		{
			get;
			set;
		}

		public int Bottom
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Left = reader.ReadInt32();
			this.Top = reader.ReadInt32();
			this.Right = reader.ReadInt32();
			this.Bottom = reader.ReadInt32();
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
