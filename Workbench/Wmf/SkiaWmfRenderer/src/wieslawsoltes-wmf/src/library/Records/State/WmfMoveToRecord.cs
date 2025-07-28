using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	//NOTE: Even if WmfMoveToRecord has the same parameters as WmfLineToRecord
	//it must NOT inherit it because it may cause unexpected behaviours when reading
	//or comparing. Inheriting should always be done from WmfBinaryRecord.
	[WmfRecord(Type = RecordType.META_MOVETO, Size = 5)]
	public class WmfMoveToRecord : WmfBinaryRecord
	{
		public WmfMoveToRecord() : base()
		{
		}

		public short X
		{
			get;
			set;
		}

		public short Y
		{
			get;
			set;
		}

		public void SetDestination(Point point)
		{
			this.X = Convert.ToInt16(point.X);
			this.Y = Convert.ToInt16(point.Y);
		}

		public Point GetDestination()
		{
			return new Point(this.X, this.Y);
		}

		public override void Read(BinaryReader reader)
		{
			Y = reader.ReadInt16();
			X = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			writer.Write(Y);
			writer.Write(X);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.Append("X: " + this.X);
			builder.Append("Y: " + this.Y);
		}
	}
}
