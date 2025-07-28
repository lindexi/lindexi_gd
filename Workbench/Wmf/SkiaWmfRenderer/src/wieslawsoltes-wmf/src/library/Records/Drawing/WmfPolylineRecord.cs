using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_POLYLINE, SizeIsVariable = true)]
	public class WmfPolylineRecord : WmfBinaryRecord
	{
		public WmfPolylineRecord() : base()
		{
			this.Points = new List<Point>();
		}

		public short NumberOfPoints
		{
			get;
			set;
		}

		public List<Point> Points
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.Points = new List<Point>();
			this.NumberOfPoints = reader.ReadInt16();
			for (int i = 0; i < this.NumberOfPoints; i++)
			{
				short x = reader.ReadInt16();
				short y = reader.ReadInt16();
				this.Points.Add(new Point(x, y));
			}
		}

		public override void Write(BinaryWriter writer)
		{
			if (this.Points.Count != this.NumberOfPoints)
			{
				throw new WmfException(this.RecordType.ToString() + ".NumberOfPoints does not match with actual number of points!");
			}

			//Record size is variable
			base.RecordSizeBytes = (uint)(4 /* RecordSize */ + 2 /* RecordType */ + 2 /* NumberOfPoints */ + 4 * this.NumberOfPoints);
			base.Write(writer);

			writer.Write(this.NumberOfPoints);
			foreach (var point in this.Points)
			{
				writer.Write((short)point.X);
				writer.Write((short)point.Y);
			}
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("NumberOfPoints: " + this.NumberOfPoints);
			builder.AppendLine("aPoints:");

			for (int i = 0; i < this.Points.Count; i++)
			{
				var point = this.Points[i];
				//builder.AppendFormat("P{0:0000}: X = {1}, Y = {2}", i, point.X, point.Y).AppendLine();
				builder.AppendFormat("{0}, {1}", point.X, point.Y).AppendLine(); //Simplified
			}
		}
	}
}
