using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 4)]
	public class PaletteEntry : WmfBinaryObject
	{
		public const uint SizeBytes = 4;

		public PaletteEntry() : base()
		{
		}

		public PaletteEntryFlag Values
		{
			get;
			set;
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

		public override void Read(BinaryReader reader)
		{
			this.Values = (PaletteEntryFlag)reader.ReadByte();
			this.Blue = reader.ReadByte();
			this.Green = reader.ReadByte();
			this.Red = reader.ReadByte();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write((byte)this.Values);
			writer.Write(this.Blue);
			writer.Write(this.Green);
			writer.Write(this.Red);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tValues: " + this.Values);
			builder.AppendLine("\tBlue: " + this.Blue);
			builder.AppendLine("\tGreen: " + this.Green);
			builder.AppendLine("\tRed: " + this.Red);
		}
	}
}
