using System;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	/// <summary>
	/// Implements a WMF META record. Each inherited record should also define WmfRecordAttribute above the class definition.
	/// NOTE: Always inherit META record directly from this class to avoid conflicts with different WmfRecordAttribute values.
	/// </summary>
	public abstract class WmfBinaryRecord : IBinaryRecord
	{
		#region Constructor
		public WmfBinaryRecord()
		{
			var attribute = Attribute.GetCustomAttribute(this.GetType(), typeof(WmfRecordAttribute)) as WmfRecordAttribute;
			if (attribute != null)
			{
				this.RecordType = attribute.Type;
				if (!attribute.SizeIsVariable)
					this.RecordSize = attribute.Size;
			}
		}
		#endregion

		#region Public properties
		/// <summary>
		/// Gets or sets record length in 16-bit words.
		/// </summary>
		public uint RecordSize
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets record length in bytes.
		/// </summary>
		public uint RecordSizeBytes
		{
			get
			{
				return this.RecordSize * 2;
			}
			set
			{
				this.RecordSize = value / 2;
			}
		}

		/// <summary>
		/// Gets or sets record type (aka RecordFunction)
		/// </summary>
		public RecordType RecordType
		{
			get;
			set;
		}
		#endregion

		#region Public methods
		public override string ToString()
		{
			return Dump() ?? this.RecordType.ToString();
		}
		#endregion

		#region IBinaryRecord Members
		/// <summary>
		/// Reads a record from binary stream. If this method is not overridden it will skip this record and go to next record.
		/// NOTE: When overriding this method remove the base.Read(reader) line from code.
		/// </summary>
		/// <param name="reader"></param>
		public virtual void Read(BinaryReader reader)
		{
			//RecordSize and RecordType should already be set by WmfReader

			//Skip record if not overridden
			var length = this.RecordSizeBytes - 6; //Size without RecordSize and RecordType field
			if (length > 0)
			{
				reader.BaseStream.Seek(0, SeekOrigin.Current);
			}
		}

		/// <summary>
		/// Writes a record to binary stream. This method must be overridden if RecordSize > 3 and
		/// should include base.Write(writer) in overridden method (unlike Read where base method
		/// should be removed)
		/// </summary>
		/// <param name="writer"></param>
		public virtual void Write(BinaryWriter writer)
		{
			if (this.RecordSize < 3)
			{
				throw new WmfException("RecordSize cannot be lower than 3 WORDs! Record: " + this.GetType());
			}

			writer.Write((uint)this.RecordSize);
			writer.Write((ushort)this.RecordType);
		}

		public string Dump()
		{
			var builder = new StringBuilder();
			Dump(builder);
			return builder.ToString();
		}

		protected virtual void Dump(StringBuilder builder)
		{
			builder.AppendFormat("== {0} ==", this.GetType().Name).AppendLine();
			builder.AppendFormat("RecordSize: {0} words = {1} bytes", this.RecordSize, this.RecordSizeBytes).AppendLine();
			builder.AppendFormat("RecordType: 0x{0:x4} (RecordType.{1})", (ushort)this.RecordType, this.RecordType.ToString()).AppendLine();
			//Other fields should be added in overridden method
		}
		#endregion
	}
}
