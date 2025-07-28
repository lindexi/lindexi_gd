using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 12)]
	public class ColorRef : WmfBinaryObject
	{
		public ColorRef() : base()
		{
		}

		public byte Red
		{
			get;
			set;
		}

		public byte Green
		{
			get;
			set;
		}

		public byte Blue
		{
			get;
			set;
		}

		public byte Reserved
		{
			get
			{
				return 0x00; //Must be 0x00
			}
		}

		public override void Read(BinaryReader reader)
		{
			this.Red = reader.ReadByte();
			this.Green = reader.ReadByte();
			this.Blue = reader.ReadByte();
			byte reserved = reader.ReadByte();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Red);
			writer.Write(this.Green);
			writer.Write(this.Blue);
			writer.Write(this.Reserved);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tRed: " + this.Red);
			builder.AppendLine("\tGreen: " + this.Green);
			builder.AppendLine("\tBlue: " + this.Blue);
			builder.AppendLine("\tReserved: " + this.Reserved);
		}

		public static implicit operator Color(ColorRef cref)
		{
			return Color.FromArgb(cref.Red, cref.Green, cref.Blue);
		}

		public static implicit operator ColorRef(Color color)
		{
			return new ColorRef()
			{
				Red = color.R,
				Green = color.G,
				Blue = color.B
			};
		}
	}
}
