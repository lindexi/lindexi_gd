using System.IO;
using System.Text;

namespace Oxage.Wmf.Objects
{
	[WmfObject(Size = 124)]
	public class BitmapV5Header : WmfBinaryObject
	{
		public BitmapV5Header() : base()
		{
			this.BitmapV4Header = new BitmapV4Header();
		}

		public BitmapV4Header BitmapV4Header
		{
			get;
			set;
		}

		public LogicalColorSpace Intent
		{
			get;
			set;
		}

		public uint ProfileData
		{
			get;
			set;
		}

		public uint ProfileSize
		{
			get;
			set;
		}

		public uint Reserved
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			this.BitmapV4Header = reader.ReadWmfObject<BitmapV4Header>();
			this.Intent = (LogicalColorSpace)reader.ReadUInt32();
			this.ProfileData = reader.ReadUInt32();
			this.ProfileSize = reader.ReadUInt32();
			this.Reserved = reader.ReadUInt32();
		}

		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.BitmapV4Header);
			writer.Write((uint)this.Intent);
			writer.Write(this.ProfileData);
			writer.Write(this.ProfileSize);
			writer.Write(this.Reserved);
		}

		public override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("\tBitmapV4Header: ").Append(this.BitmapV4Header.Dump());
			builder.AppendLine("\tIntent: " + this.Intent);
			builder.AppendLine("\tProfileData: " + this.ProfileData);
			builder.AppendLine("\tProfileSize: " + this.ProfileSize);
			builder.AppendLine("\tReserved: " + this.Reserved);
		}
	}
}
