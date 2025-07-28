using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	//NOTE: Even if WmfEllipseRecord has the same parameters as WmfRectangleRecord
	//it must NOT inherit it because it may cause unexpected behaviours when reading
	//or comparing. Inheriting should always be done from WmfBinaryRecord.
	[WmfRecord(Type = RecordType.META_ELLIPSE, Size = 7)]
	public class WmfEllipseRecord : WmfBinaryRecord
	{
		public WmfEllipseRecord() : base()
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

		public void SetEllipse(Point center, Point radius)
		{
			this.SetRectangle(new Rectangle(
					new Point(center.X - radius.X, center.Y - radius.Y),
					new Size(radius.X + radius.X, radius.Y + radius.Y)
					));
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
