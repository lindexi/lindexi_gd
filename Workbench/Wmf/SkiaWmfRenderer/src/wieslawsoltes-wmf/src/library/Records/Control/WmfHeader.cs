using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	public class WmfHeader : IBinaryRecord
	{
		#region Constructor
		public WmfHeader()
		{
			this.Type = MetafileType.MEMORYMETAFILE;
			this.HeaderSize = 9;
			this.Version = 0x0300;
		}
		#endregion

		#region Public properties
		public MetafileType Type
		{
			get;
			set;
		}

		public ushort HeaderSize
		{
			get;
			set;
		}

		public ushort Version
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets number of WORDs in the entire WMF.
		/// </summary>
		public uint FileSize
		{
			get;
			set;
		}

		public ushort NumberOfObjects
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets size in WORDs of the largest record in WMF.
		/// </summary>
		public uint MaxRecord
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets number of members. This value should always be 0x0000 according to the WMF specification.
		/// </summary>
		public ushort NumberOfMembers
		{
			get;
			set;
		}
		#endregion

		#region Public methods
		public override string ToString()
		{
			return Dump();
		}
		#endregion

		#region IBinaryRecord Members
		public void Read(BinaryReader reader)
		{
			this.Type = (MetafileType)reader.ReadUInt16();
			this.HeaderSize = reader.ReadUInt16();
			this.Version = reader.ReadUInt16();
			this.FileSize = reader.ReadUInt32();
			this.NumberOfObjects = reader.ReadUInt16();
			this.MaxRecord = reader.ReadUInt32();
			this.NumberOfMembers = reader.ReadUInt16();
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write((ushort)this.Type);
			writer.Write(this.HeaderSize);
			writer.Write(this.Version);
			writer.Write(this.FileSize);
			writer.Write(this.NumberOfObjects);
			writer.Write(this.MaxRecord);
			writer.Write(this.NumberOfMembers);
		}

		public string Dump()
		{
			var builder = new StringBuilder();
			builder.AppendLine("== WmfHeader ==");
			builder.AppendFormat("Type: 0x{0:x4} (MetafileType.{1})", (ushort)this.Type, this.Type.ToString()).AppendLine();
			builder.AppendFormat("HeaderSize: {0}", this.HeaderSize).AppendLine();
			builder.AppendFormat("Version: 0x{0:x4}", this.Version).AppendLine();
			builder.AppendFormat("FileSize: {0} words = {1} bytes", this.FileSize, this.FileSize * 2).AppendLine();
			builder.AppendFormat("NumberOfObjects: {0}", this.NumberOfObjects).AppendLine();
			builder.AppendFormat("MaxRecord: {0} words = {1} bytes", this.MaxRecord, this.MaxRecord * 2).AppendLine();
			builder.AppendFormat("NumberOfMembers: {0}", this.NumberOfMembers).AppendLine();
			return builder.ToString();
		}
		#endregion
	}
}
