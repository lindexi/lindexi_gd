using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_POLYPOLYGON, SizeIsVariable = true)]
	public class WmfPolyPolygonRecord : WmfBinaryRecord
	{
		public WmfPolyPolygonRecord() : base()
		{
			this.NumberOfPolygons = 0;
			this.PointsPerPolygon = new List<int>();
			this.Points = new List<Point>();
		}

		public short NumberOfPolygons
		{
			get;
			set;
		}

		public List<int> PointsPerPolygon
		{
			get;
			set;
		}

		public List<Point> Points
		{
			get;
			set;
		}

		public int GetPointsCount()
		{
			return this.PointsPerPolygon.Sum(x => x);
		}

		public override void Read(BinaryReader reader)
		{
			this.Points = new List<Point>();
			this.PointsPerPolygon = new List<int>();
			this.NumberOfPolygons = reader.ReadInt16();

			int count = 0;
			for (int i = 0; i < this.NumberOfPolygons; i++)
			{
				ushort n = reader.ReadUInt16();
				this.PointsPerPolygon.Add((int)n);
				count += n;
			}

			for (int i = 0; i < count; i++)
			{
				short x = reader.ReadInt16();
				short y = reader.ReadInt16();
				this.Points.Add(new Point(x, y));
			}
		}

		public override void Write(BinaryWriter writer)
		{
			if (this.PointsPerPolygon.Count != this.NumberOfPolygons)
			{
				throw new WmfException(this.RecordType.ToString() + ".NumberOfPolygons does not match with actual number of polygons!");
			}

			//Record size is variable
			base.RecordSizeBytes = (uint)(4 /* RecordSize */ + 2 /* RecordType */ + 2 /* NumberOfPoints */ + 2 * this.NumberOfPolygons + 4 * GetPointsCount());
			base.Write(writer);

			writer.Write(this.NumberOfPolygons);

			foreach (var n in this.PointsPerPolygon)
			{
				writer.Write((ushort)n);
			}

			foreach (var point in this.Points)
			{
				writer.Write((short)point.X);
				writer.Write((short)point.Y);
			}
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("NumberOfPolygons: " + this.NumberOfPolygons);

			builder.AppendLine("PointsPerPolygon:");
			for (int i = 0; i < this.PointsPerPolygon.Count; i++)
			{
				int count = this.PointsPerPolygon[i];
				builder.AppendFormat("{0}", count).AppendLine();
			}
			
			builder.AppendLine("Points:");
			for (int i = 0; i < this.Points.Count; i++)
			{
				var point = this.Points[i];
				builder.AppendFormat("{0}, {1}", point.X, point.Y).AppendLine();
			}
		}
	}
}
