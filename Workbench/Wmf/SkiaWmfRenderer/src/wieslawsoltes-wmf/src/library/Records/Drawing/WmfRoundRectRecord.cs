using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_ROUNDRECT, Size = 9)]
	public class WmfRoundRectRecord : WmfBinaryRecord
	{
		public WmfRoundRectRecord() : base()
		{
		}

		public short Width
		{
			get;
			set;
		}

		public short Height
		{
			get;
			set;
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

		public void SetRectangle(Rectangle rect, int cornerRadius = 0)
		{
			this.Width = (short)cornerRadius;
			this.Height = (short)cornerRadius;
			this.TopRect = (short)rect.Top;
			this.LeftRect = (short)rect.Left;
			this.BottomRect = (short)rect.Bottom;
			this.RightRect = (short)rect.Right;
		}

		public Rectangle GetRectangle()
		{
			return new Rectangle(this.LeftRect, this.TopRect, this.RightRect - this.LeftRect, this.BottomRect - this.TopRect);
		}

		/// <summary>
		/// Gets corner radius. See remarks.
		/// </summary>
		/// <remarks>
		/// Assumes that Width equals Height.
		/// </remarks>
		/// <returns></returns>
		public int GetCornerRadius()
		{
			return this.Width;
		}

		public override void Read(BinaryReader reader)
		{
			this.Width = reader.ReadInt16();
			this.Height = reader.ReadInt16();
			this.BottomRect = reader.ReadInt16();
			this.RightRect = reader.ReadInt16();
			this.TopRect = reader.ReadInt16();
			this.LeftRect = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.Width);
			writer.Write(this.Height);
			writer.Write(this.BottomRect);
			writer.Write(this.RightRect);
			writer.Write(this.TopRect);
			writer.Write(this.LeftRect);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Width: " + this.Width);
			builder.AppendLine("Height: " + this.Height);
			builder.AppendLine("BottomRect: " + this.BottomRect);
			builder.AppendLine("RightRect: " + this.RightRect);
			builder.AppendLine("TopRect: " + this.TopRect);
			builder.AppendLine("LeftRect: " + this.LeftRect);
		}
	}
}
