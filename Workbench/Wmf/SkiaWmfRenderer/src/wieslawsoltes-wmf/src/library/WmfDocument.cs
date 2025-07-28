using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Oxage.Wmf.Records;

namespace Oxage.Wmf
{
	//Human-friendly reader and writer
	public class WmfDocument
	{
		public WmfDocument()
		{
			this.Records = new List<IBinaryRecord>();
			this.Records.Add(new WmfFormat());
			this.Records.Add(new WmfHeader());
			this.Records.Add(new WmfSetMapModeRecord());
			this.Records.Add(new WmfSetWindowOrgRecord());
			this.Records.Add(new WmfSetWindowExtRecord()); //Add SetWindowExtRecord for compatibility
		}

		/// <summary>
		/// Get the current position
		/// </summary>
		public Point Position { get; private set; }

		public List<IBinaryRecord> Records
		{
			get;
			set;
		}

		public WmfFormat Format
		{
			get
			{
				return this.Records.FirstOrDefault(x => x is WmfFormat) as WmfFormat;
			}
		}

		public WmfHeader Header
		{
			get
			{
				return this.Records.FirstOrDefault(x => x is WmfHeader) as WmfHeader;
			}
		}

		public int Width
		{
			get
			{
				return this.Format.Right - this.Format.Left;
			}
			set
			{
				this.Format.Right = (ushort)(this.Format.Left + value);

				var ext = this.Records.FirstOrDefault(x => x is WmfSetWindowExtRecord) as WmfSetWindowExtRecord;
				if (ext != null)
				{
					ext.X = (short)(this.Format.Right - this.Format.Left);
				}
			}
		}

		public int Height
		{
			get
			{
				return this.Format.Bottom - this.Format.Top;
			}
			set
			{
				this.Format.Bottom = (ushort)(this.Format.Top + value);

				var ext = this.Records.FirstOrDefault(x => x is WmfSetWindowExtRecord) as WmfSetWindowExtRecord;
				if (ext != null)
				{
					ext.Y = (short)(this.Format.Bottom - this.Format.Top);
				}
			}
		}

		public void Load(string path)
		{
			using (var stream = File.OpenRead(path))
			{
				Load(stream);
			}
		}

		public void Load(Stream stream)
		{
			this.Records = new List<IBinaryRecord>();
			using (var reader = new WmfReader(stream))
			{
				while (!reader.IsEndOfFile)
				{
					try
					{
						var record = reader.Read();
						if (record != null)
							this.Records.Add(record);
					}
					catch (EndOfStreamException)
					{
						//End of stream
						break;
					}
				}
			}
		}

		public void Save(string path)
		{
			using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
			{
				Save(stream);
			}
		}

		public void Save(Stream stream)
		{
			this.Format.Checksum = this.Format.CalculateChecksum();

			if (!this.Records.OfType<WmfEndOfFileRecord>().Any())
			{
				this.Records.Add(new WmfEndOfFileRecord());
			}

			using (var writer = new WmfWriter(stream))
			{
				foreach (var record in this.Records)
				{
					writer.Write(record);
				}

				writer.FixHeader();
				writer.FixPadding();
			}
		}

		public string Dump()
		{
			var builder = new StringBuilder();
			foreach (var record in this.Records)
			{
				builder.AppendLine(record.Dump());
				builder.AppendLine();
			}
			return builder.ToString();
		}

		/// <summary>
		/// Move the current position to the destination
		/// </summary>
		/// <param name="destination"></param>
		public void MoveTo(Point destination)
		{
			var record = new WmfMoveToRecord();
			record.SetDestination(destination);
			this.Records.Add(record);
			this.Position = destination;
		}

		public void AddSelectObject(int index)
		{
			this.Records.Add(new WmfSelectObjectRecord() { ObjectIndex = (ushort)index });
		}

		public void AddDeleteObject(int index)
		{
			this.Records.Add(new WmfDeleteObjectRecord() { ObjectIndex = (ushort)index });
		}

		/// <summary>
		/// Add a line from start to end.
		/// </summary>
		/// <param name="start">Starting Point</param>
		/// <param name="end">Ending Point</param>
		public void AddLine(Point start, Point end)
		{
			var oldPosition = this.Position;
			MoveTo(start);
			AddLineTo(end.X, end.Y);
			MoveTo(oldPosition);
		}

		/// <summary>
		/// Add a line from current Position to (x,y)
		/// </summary>
		/// <param name="destination"></param>
		public WmfLineToRecord AddLineTo(int x, int y)
		{
			var record = new WmfLineToRecord();
			record.SetDestination(new Point(x, y));
			this.Records.Add(record);
			return record;
		}

		public WmfPolylineRecord AddPolyline(IEnumerable<Point> points)
		{
			var list = points.ToList();
			var record = new WmfPolylineRecord()
			{
				NumberOfPoints = (short)list.Count,
				Points = list
			};
			this.Records.Add(record);
			return record;
		}

		public WmfPolygonRecord AddPolygon(IEnumerable<Point> points)
		{
			var list = points.ToList();
			var record = new WmfPolygonRecord()
			{
				NumberOfPoints = (short)list.Count,
				Points = list
			};
			this.Records.Add(record);
			return record;
		}

		public WmfPolyPolygonRecord AddPolyPolygon(IEnumerable<IEnumerable<Point>> polygons)
		{
			var list = polygons.ToList();

			var record = new WmfPolyPolygonRecord();
			record.NumberOfPolygons = (short)list.Count;
			record.PointsPerPolygon = new List<int>();
			record.Points = new List<Point>();

			foreach (var polygon in list)
			{
				var points = polygon.ToList();
				record.PointsPerPolygon.Add(points.Count);

				foreach (var point in polygon)
				{
					record.Points.Add(point);
				}
			}

			this.Records.Add(record);
			return record;
		}

		public IBinaryRecord AddRectangle(int x, int y, int width, int height, int cornerRadius = 0)
		{
			return AddRectangle(new Rectangle(x, y, width, height), cornerRadius);
		}

		public IBinaryRecord AddRectangle(Point corner, Size size, int cornerRadius = 0)
		{
			return AddRectangle(new Rectangle(corner.X, corner.Y, size.Width, size.Height), cornerRadius);
		}

		public IBinaryRecord AddRectangle(Rectangle rect, int cornerRadius = 0)
		{
			if (cornerRadius > 0)
			{
				//Rounded rectangle
				var record = new WmfRoundRectRecord();
				record.SetRectangle(rect, cornerRadius);
				this.Records.Add(record);
				return record;
			}
			else
			{
				//Classic rectangle
				var record = new WmfRectangleRecord();
				record.SetRectangle(rect);
				this.Records.Add(record);
				return record;
			}
		}

		/// <summary>
		/// Add an ellipse by specifying its bounding rectangle.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public WmfEllipseRecord AddEllipse(int x, int y, int width, int height)
		{
			var record = new WmfEllipseRecord();
			record.SetRectangle(new Rectangle(x, y, width, height));
			this.Records.Add(record);
			return record;
		}

		/// <summary>
		/// Add an ellipse by specifying its center and x/y radius
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		public WmfEllipseRecord AddEllipse(Point center, Point radius)
		{
			var record = new WmfEllipseRecord();
			record.SetEllipse(center, radius);
			this.Records.Add(record);
			return record;
		}

		/// <summary>
		/// Add a circle (equi-radius ellipse) by specifying its center and radius
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		public WmfEllipseRecord AddCircle(int x, int y, int radius)
		{
			return AddEllipse(new Point(x, y), new Point(radius, radius));
		}

		/// <summary>
		/// Add a circle (equi-radius ellipse) by specifying its center and radius
		/// </summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		public WmfEllipseRecord AddCircle(Point center, int radius)
		{
			return AddEllipse(center, new Point(radius, radius));
		}

		/// <summary>
		/// Draws an arc. Doesn't seem to preserve shape when ungrouped in excel.
		/// </summary>
		/// <param name="rectangle"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public WmfArcRecord AddArc(Rectangle rectangle, Point start, Point end)
		{
			var record = new WmfArcRecord();
			record.SetArc(rectangle, start, end);
			this.Records.Add(record);
			return record;
		}

		public WmfSetPolyFillModeRecord AddPolyFillMode(PolyFillMode mode)
		{
			var record = new WmfSetPolyFillModeRecord() { Mode = mode };
			this.Records.Add(record);
			return record;
		}

		public WmfCreateBrushIndirectRecord AddCreateBrushIndirect(Color color, BrushStyle style = BrushStyle.BS_SOLID, HatchStyle hatch = HatchStyle.HS_HORIZONTAL)
		{
			var record = new WmfCreateBrushIndirectRecord()
			{
				Color = color,
				Style = style,
				Hatch = hatch
			};

			this.Records.Add(record);
			return record;
		}

		public WmfCreatePenIndirectRecord AddCreatePenIndirect(Color color, PenStyle style = PenStyle.PS_SOLID, int size = 1)
		{
			var record = new WmfCreatePenIndirectRecord()
			{
				Color = color,
				Style = style,
				Width = new Point(size, size)
			};

			this.Records.Add(record);
			return record;
		}

		public WmfCreateFontIndirectRecord AddCreateFontIndirect(string fontName, int size, int weight = 400, bool italic = false, bool underline = false)
		{
			var record = new WmfCreateFontIndirectRecord()
			{
				Height = (short)size,
				Weight = (short)weight,
				Italic = italic,
				Underline = underline,
				FaceName = fontName
			};

			this.Records.Add(record);
			return record;
		}

		public WmfSetTextAlignRecord AddTextAlignment(TextAlignmentMode mode)
		{
			var record = new WmfSetTextAlignRecord() { Mode = mode };
			this.Records.Add(record);
			return record;
		}

		public WmfSetTextColorRecord AddTextColor(Color color)
		{
			var record = new WmfSetTextColorRecord() { Color = color };
			this.Records.Add(record);
			return record;
		}

		public WmfTextoutRecord AddText(string text, int x = 0, int y = 0)
		{
			var record = new WmfTextoutRecord()
			{
				StringValue = text,
				XStart = (short)x,
				YStart = (short)y
			};
			this.Records.Add(record);
			return record;
		}

		public WmfTextoutRecord AddText(string text, Point start)
		{
			return AddText(text, start.X, start.Y);
		}
	}
}
