using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(SizeIsVariable = true)]
	public class Palette : WmfBinaryObject
	{
		public Palette(RecordType rt) : base()
		{
			if (rt == RecordType.META_CREATEPALETTE)
				this.Start = (short)0x300;
		}

		public short Start
		{
			get;
			set;
		}

		public short NumberOfEntries
		{
			get;
			set;
		}

		public PaletteEntry[] PaletteEntries
		{
			get;
			set;
		}

		public uint SizeBytes
		{
			get
			{
				if (PaletteEntries == null)
					return 4;
				else
					return 4 + (uint)PaletteEntries.Length * PaletteEntry.SizeBytes;
			}
		}

		public override int GetSize()
		{
			return (int)this.SizeBytes;
		}

		public override void Read(BinaryReader reader)
		{
			this.Start = reader.ReadInt16();
			this.NumberOfEntries = reader.ReadInt16();
			PaletteEntries = new PaletteEntry[this.NumberOfEntries];
			for (int i = 0; i < NumberOfEntries; i++)
			{
				PaletteEntries[i] = new PaletteEntry();
				PaletteEntries[i].Read(reader);
			}
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Start);
			if (PaletteEntries == null)
			{
				writer.Write((short)0);
			}
			else
			{
				writer.Write((short)this.PaletteEntries.Length);
				foreach (var paletteEntry in PaletteEntries)
					paletteEntry.Write(writer);
			}
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tStart: " + this.Start);
			builder.AppendLine("\tNumberOfEntries: " + this.NumberOfEntries);
			if (this.PaletteEntries != null)
				foreach (var paletteEntry in PaletteEntries)
					paletteEntry.Dump(builder);
		}
	}
}
