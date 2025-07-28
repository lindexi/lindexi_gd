using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_ARC, Size = 11)]
	public class WmfArcRecord : WmfBinaryRecord
	{
		public WmfArcRecord() : base()
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

		public short YEndArc
		{
			get;
			set;
		}

		public short XEndArc
		{
			get;
			set;
		}

		public short YStartArc
		{
			get;
			set;
		}

		public short XStartArc
		{
			get;
			set;
		}

		public void SetArc(Rectangle rectangle, Point start, Point end)
		{
			this.YEndArc = (short)(end.Y);
			this.XEndArc = (short)(end.X);
			this.YStartArc = (short)(start.Y);
			this.XStartArc = (short)(start.X);

			this.BottomRect = (short)rectangle.Bottom;
			this.RightRect = (short)rectangle.Right;
			this.TopRect = (short)rectangle.Top;
			this.LeftRect = (short)rectangle.Left;
		}

		public override void Read(BinaryReader reader)
		{
			this.YEndArc = reader.ReadInt16();
			this.XEndArc = reader.ReadInt16();
			this.YStartArc = reader.ReadInt16();
			this.XStartArc = reader.ReadInt16();

			this.BottomRect = reader.ReadInt16();
			this.RightRect = reader.ReadInt16();
			this.TopRect = reader.ReadInt16();
			this.LeftRect = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.YEndArc);
			writer.Write(this.XEndArc);
			writer.Write(this.YStartArc);
			writer.Write(this.XStartArc);

			writer.Write(this.BottomRect);
			writer.Write(this.RightRect);
			writer.Write(this.TopRect);
			writer.Write(this.LeftRect);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("YEndArc: " + this.YEndArc);
			builder.AppendLine("XEndArc: " + this.XEndArc);
			builder.AppendLine("YStartArc: " + this.YStartArc);
			builder.AppendLine("XStartArc: " + this.XStartArc);
			builder.AppendLine("BottomRect: " + this.BottomRect);
			builder.AppendLine("RightRect: " + this.RightRect);
			builder.AppendLine("TopRect: " + this.TopRect);
			builder.AppendLine("LeftRect: " + this.LeftRect);
		}
	}
}
