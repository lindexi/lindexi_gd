using System;
using System.IO;
using Oxage.Wmf.Records;

namespace Oxage.Wmf
{
	/// <summary>
	/// Low-level WMF parser
	/// </summary>
	public class WmfReader : IDisposable
	{
		private Stream stream;
		private BinaryReader reader;

		public WmfReader(Stream stream)
		{
			this.stream = stream;
			this.reader = new BinaryReader(stream);
        }

		public bool IsFormatRead
		{
			get;
			protected set;
		}

		public bool IsHeaderRead
		{
			get;
			protected set;
		}

		public bool IsEndOfFile
		{
			get { return stream.Length == stream.Position; }
		}

		public IBinaryRecord Read()
		{
			if (!this.IsFormatRead)
			{
				this.IsFormatRead = true;
				var format = new WmfFormat();
				format.Read(reader);
				return format;
			}

			if (!this.IsHeaderRead)
			{
				this.IsHeaderRead = true;
				var header = new WmfHeader();
				header.Read(reader);
				return header;
			}

			long begin = reader.BaseStream.Position;

			uint length = reader.ReadUInt32(); //Length in WORDs
			ushort type = reader.ReadUInt16();

			var rt = (RecordType)type;
			var record = WmfHelper.GetRecordByType(rt) as WmfBinaryRecord;

			if (record == null)
			{
				record = new WmfUnknownRecord();
				record.RecordType = rt; //Only set for UnknownRecord otherwise it is already defined by attribute above the class
			}

			record.RecordSize = length;
			record.Read(reader);

			long end = reader.BaseStream.Position;
			long rlen = end - begin; //Read length
			long excess = 2 * length - rlen;
			if (excess > 0)
			{
				//Oops, reader did not read whole record?!
				reader.Skip((int)excess);
			}

			return record;
		}

		public void Dispose()
		{
			if (this.reader != null)
			{
				this.reader.Close();
				this.reader = null;
			}
		}
	}
}
