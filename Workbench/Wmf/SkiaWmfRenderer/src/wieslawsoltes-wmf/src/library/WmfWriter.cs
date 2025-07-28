using System;
using System.IO;
using Oxage.Wmf.Records;

namespace Oxage.Wmf
{
	/// <summary>
	/// Low-level WMF builder
	/// </summary>
	public class WmfWriter : IDisposable
	{
		private Stream stream;
		private BinaryWriter writer;

		public WmfWriter(Stream stream)
		{
			this.stream = stream;
			this.writer = new BinaryWriter(stream);
		}

		public void Write(IBinaryRecord record)
		{
			record.Write(writer);
		}

		/// <summary>
		/// Fixes file size field, max records field and number of objects field.
		/// </summary>
		public void FixHeader()
		{
			if (!stream.CanSeek)
			{
				throw new WmfException("Cannot fix WMF header since the stream does not support seeking!");
			}

			//Skip WmfFormat record
			stream.Seek(22, SeekOrigin.Begin);

			//Read whole WmfHeader record
			byte[] buffer = new byte[18];
			stream.Read(buffer, 0, 18);

			//Create new header record
			var header = new WmfHeader();
			using (var ms = new MemoryStream(buffer))
			{
				using (var reader = new BinaryReader(ms))
				{
					uint max, count;
					Find(out max, out count);
					
					long l = stream.Length - 22; //Length without WmfFormat in bytes
					if (l % 2 == 1) l += 1; //Round to WORD padding
					long size = l / 2; //Length in words

					header.Read(reader);
					header.FileSize = (uint)size;
					header.NumberOfObjects = (ushort)count;
					header.MaxRecord = max;
				}
			}

			byte[] fix = new byte[18];
			using (var ms = new MemoryStream())
			{
				using (var writer = new BinaryWriter(ms))
				{
					header.Write(writer);
					fix = ms.ToArray();
				}
			}

			//Rewind back to position of overwrite
			this.stream.Seek(22, SeekOrigin.Begin);
			this.writer.Write(fix);

			//Forward to when fixing started
			stream.Seek(0, SeekOrigin.End);
		}

		protected void Find(out uint maxRecordSize, out uint numberOfObjects)
		{
			maxRecordSize = 0;
			numberOfObjects = 0;

			//Skip WmfFormat and WmfHeader record
			stream.Seek(22 + 18, SeekOrigin.Begin);

			var reader = new BinaryReader(stream);
			//using (var reader = new BinaryReader(stream)) //Do NOT use 'using' here as will close the stream

			while (true)
			{
				try
				{
					//End of stream
					if (stream.Position == stream.Length)
					{
						break;
					}

					//Read record size in WORDs and record type
					uint length = reader.ReadUInt32();
					ushort type = reader.ReadUInt16();
					int offset = 4 + 2; //4 bytes read for 'length' and 2 for 'type'

					//Find maximum record size
					maxRecordSize = Math.Max(length, maxRecordSize);

					//Find maximum object index, since indices are sorted incrementally it is assumed that index + 1 means number of objects
					if (type == (ushort)RecordType.META_SELECTOBJECT || type == (ushort)RecordType.META_DELETEOBJECT)
					{
						ushort objectIndex = reader.ReadUInt16();
						numberOfObjects = Math.Max((uint)objectIndex + 1, numberOfObjects);
						offset += 2;
					}

					//Skip record
					stream.Seek(2 * length - offset, SeekOrigin.Current);
				}
				catch (EndOfStreamException)
				{
					//End of stream
					break;
				}
			}

			//Forward to when fixing started
			stream.Seek(0, SeekOrigin.End);
		}

		/// <summary>
		/// Fixes padding to align to 16-bit boundary.
		/// </summary>
		public void FixPadding()
		{
			//WMF must align to 16-bit (? not sure, cannot find in docs but it's logical since size value is in 16-bit worda)
			if (stream.Length % 2 == 1)
			{
				writer.Write((byte)0);
			}
		}

		public void Dispose()
		{
			if (this.writer != null)
			{
				this.writer.Close();
				this.writer = null;
			}
		}
	}
}
