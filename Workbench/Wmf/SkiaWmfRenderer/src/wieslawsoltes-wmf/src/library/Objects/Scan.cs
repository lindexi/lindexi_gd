using System;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(SizeIsVariable = true)]
	public class Scan : WmfBinaryObject
	{
		public Scan() : base()
		{
		}

		public ushort Count
		{
			get;
			set;
		}

		public ushort Top
		{
			get;
			set;
		}

		public ushort Bottom
		{
			get;
			set;
		}

		public byte[] ScanLines
		{
			get;
			set;
		}

		public override int GetSize()
		{
			return 8 /* Fixed size fields */ + (this.ScanLines != null ? this.ScanLines.Length : 0);
		}

		public override void Read(BinaryReader reader)
		{
			this.Count = reader.ReadUInt16();
			this.Top = reader.ReadUInt16();
			this.Bottom = reader.ReadUInt16();
			this.ScanLines = reader.ReadBytes(4 * this.Count); //2 bytes for Left, 2 bytes for Right
			//TODO: Create ScanLine object with Left and Right field/property

			ushort count2 = reader.ReadUInt16();
			if (this.Count != count2)
			{
				throw new WmfException("Count and Count2 fields in Scan Object should have equal value!");
			}
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Count);
			writer.Write(this.Top);
			writer.Write(this.Bottom);
			writer.Write(this.ScanLines);
			writer.Write(this.Count); //Count2 should have the same value as Count
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tCount: " + this.Count);
			builder.AppendLine("\tTop: " + this.Top);
			builder.AppendLine("\tBottom: " + this.Bottom);
			builder.AppendLine("\tScanLines: " + WmfHelper.DumpByteArray(this.ScanLines));
			builder.AppendLine("\tCount2: " + this.Count);
		}
	}
}
