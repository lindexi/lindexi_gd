using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_RECTANGLE, Size = 7)]
	public class WmfRectangleRecord : WmfBinaryRecord
	{
		public WmfRectangleRecord() : base()
		{
		}

		public short BottomRect
		{
			get;
			set;
		}

		public short RightRect
		{
			get;
			set;
		}

		public short TopRect
		{
			get;
			set;
		}

		public short LeftRect
		{
			get;
			set;
		}

		public void SetRectangle(Rectangle rect)
		{
			this.TopRect = (short)rect.Top;
			this.LeftRect = (short)rect.Left;
			this.BottomRect = (short)rect.Bottom;
			this.RightRect = (short)rect.Right;
		}

		public Rectangle GetRectangle()
		{
			return new Rectangle(this.LeftRect, this.TopRect, this.RightRect - this.LeftRect, this.BottomRect - this.TopRect);
		}

		public override void Read(BinaryReader reader)
		{
			this.BottomRect = reader.ReadInt16();
			this.RightRect = reader.ReadInt16();
			this.TopRect = reader.ReadInt16();
			this.LeftRect = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.BottomRect);
			writer.Write(this.RightRect);
			writer.Write(this.TopRect);
			writer.Write(this.LeftRect);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("BottomRect: " + this.BottomRect);
			builder.AppendLine("RightRect: " + this.RightRect);
			builder.AppendLine("TopRect: " + this.TopRect);
			builder.AppendLine("LeftRect: " + this.LeftRect);
		}
	}
}
