using System;
using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	//[WmfRecord(Type = RecordType.META_CREATEPATTERNBRUSH, SizeIsVariable = true)]
	public class WmfCreatePatternBrushRecord : WmfBinaryRecord
	{
		public WmfCreatePatternBrushRecord() : base()
		{
		}

		public Bitmap16 Bitmap16
		{
			get;
			set;
		}

		public byte[] Bits
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			//NOTE: The documentation is confusing, uses only the first part of Bitmap16 object?!
			throw new NotImplementedException();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);

			//NOTE: The documentation is confusing, uses only the first part of Bitmap16 object?!
			throw new NotImplementedException();
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			Bitmap16.Dump(builder);
			builder.AppendLine("Bits: " + WmfHelper.DumpByteArray(this.Bits));
		}
	}
}
