using System;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(SizeIsVariable = true)]
	public class Region : WmfBinaryObject
	{
		public Region() : base()
		{
		}

		public short NextInChain
		{
			get;
			protected set;
		}

		public short ObjectType
		{
			get
			{
				return 0x0006; //Must be 0x0006
			}
		}

		public int ObjectCount
		{
			get;
			protected set;
		}

		public short RegionSize
		{
			get;
			set;
		}

		public short ScanCount
		{
			get;
			set;
		}

		public short MaxScan
		{
			get;
			set;
		}

		public Rect BoundingRectangle
		{
			get;
			set;
		}

		public Scan[] Scans
		{
			get;
			set;
		}

		public override int GetSize()
		{
			return 22 /* Fixed size fields */ +	(this.Scans != null ? this.Scans.Length * 12 : 0);
		}

		public override void Read(BinaryReader reader)
		{
			this.NextInChain = reader.ReadInt16();

			short objectType = reader.ReadInt16();
			if (objectType != this.ObjectType)
			{
				throw new WmfException("ObjectType field in Region Object must be 0x0006!");
			}

			this.ObjectCount = reader.ReadInt32();
			this.RegionSize = reader.ReadInt16();
			this.ScanCount = reader.ReadInt16();
			this.MaxScan = reader.ReadInt16();

			this.BoundingRectangle = reader.ReadWmfObject<Rect>();

			this.Scans = new Scan[this.ScanCount];
			for (int i = 0; i < this.ScanCount; i++)
			{
				this.Scans[i] = reader.ReadWmfObject<Scan>();
			}
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.NextInChain);
			writer.Write(this.ObjectType);
			writer.Write(this.ObjectCount);
			writer.Write(this.RegionSize);
			writer.Write(this.ScanCount > 0 ? this.ScanCount : (this.Scans != null ? this.Scans.Length : 0)); //Calculate ScanCount from array length
			writer.Write(this.MaxScan);
			writer.Write(this.BoundingRectangle);

			if (this.Scans != null)
			{
				foreach (var scan in this.Scans)
				{
					writer.Write(scan);
				}
			}
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);

			builder.AppendLine("\tNextInChain: " + this.NextInChain);
			builder.AppendLine("\tObjectType: " + this.ObjectType);
			builder.AppendLine("\tObjectCount: " + this.ObjectCount);
			builder.AppendLine("\tRegionSize: " + this.RegionSize);
			builder.AppendLine("\tScanCount: " + this.ScanCount);
			builder.AppendLine("\tMaxScan: " + this.MaxScan);
			builder.AppendLine("\tBoundingRectangle: ").Append(this.BoundingRectangle.Dump());

			builder.AppendLine("\tScans[" + (this.Scans != null ? this.Scans.Length : 0) + "]: ");
			if (this.Scans != null)
			{
				foreach (var scan in this.Scans)
				{
					scan.Dump(builder);
				}
			}
		}
	}
}
