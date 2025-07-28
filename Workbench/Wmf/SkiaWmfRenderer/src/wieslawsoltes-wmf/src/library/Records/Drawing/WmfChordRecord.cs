using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_CHORD, Size = 11)]
	public class WmfChordRecord : WmfBinaryRecord
	{
		public WmfChordRecord() : base()
		{
		}

		public short YRadial2
		{
			get;
			set;
		}

		public short XRadial2
		{
			get;
			set;
		}

		public short YRadial1
		{
			get;
			set;
		}

		public short XRadial1
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

		public void SetChord(Rectangle rectangle, Point firstRadial, Point secondRadial)
		{
			this.YRadial2 = (short)(secondRadial.Y);
			this.XRadial2 = (short)(secondRadial.X);
			this.YRadial1 = (short)(firstRadial.Y);
			this.XRadial1 = (short)(firstRadial.X);

			this.BottomRect = (short)rectangle.Bottom;
			this.RightRect = (short)rectangle.Right;
			this.TopRect = (short)rectangle.Top;
			this.LeftRect = (short)rectangle.Left;
		}

		public override void Read(BinaryReader reader)
		{
			this.YRadial2 = reader.ReadInt16();
			this.XRadial2 = reader.ReadInt16();
			this.YRadial1 = reader.ReadInt16();
			this.XRadial1 = reader.ReadInt16();

			this.BottomRect = reader.ReadInt16();
			this.RightRect = reader.ReadInt16();
			this.TopRect = reader.ReadInt16();
			this.LeftRect = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(this.YRadial2);
			writer.Write(this.XRadial2);
			writer.Write(this.YRadial1);
			writer.Write(this.XRadial1);

			writer.Write(this.BottomRect);
			writer.Write(this.RightRect);
			writer.Write(this.TopRect);
			writer.Write(this.LeftRect);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("YRadial2: " + this.YRadial2);
			builder.AppendLine("XRadial2: " + this.XRadial2);
			builder.AppendLine("YRadial1: " + this.YRadial1);
			builder.AppendLine("XRadial1: " + this.XRadial1);
			builder.AppendLine("BottomRect: " + this.BottomRect);
			builder.AppendLine("RightRect: " + this.RightRect);
			builder.AppendLine("TopRect: " + this.TopRect);
			builder.AppendLine("LeftRect: " + this.LeftRect);
		}
	}
}
