using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 4)]
	public class RGBQuad : WmfBinaryObject
	{
		public RGBQuad() : base()
		{
		}

		public byte Blue
		{
			get;
			set;
		}

		public byte Green
		{
			get;
			set;
		}

		public byte Red
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
			this.Blue = reader.ReadByte();
			this.Green = reader.ReadByte();
			this.Red = reader.ReadByte();
			byte reserved = reader.ReadByte();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Blue);
			writer.Write(this.Green);
			writer.Write(this.Red);
			writer.Write(this.Reserved);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tBlue: " + this.Blue);
			builder.AppendLine("\tGreen: " + this.Green);
			builder.AppendLine("\tRed: " + this.Red);
			builder.AppendLine("\tReserved: " + this.Reserved);
		}
	}
}
