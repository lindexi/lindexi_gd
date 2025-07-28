using System;
using System.IO;

namespace Oxage.Wmf
{
	public static class BinaryReaderExtensions
	{
		public static T ReadWmfObject<T>(this BinaryReader reader) where T : IBinaryObject
		{
			var result = Activator.CreateInstance<T>();
			result.Read(reader);
			return result;
		}

		/// <summary>
		/// Skips excess bytes. Work-around for some WMF files that contain undocumented fields.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="excess"></param>
		public static void Skip(this BinaryReader reader, int excess)
		{
			if (excess > 0)
			{
				//Skip unknown bytes
				reader.BaseStream.Seek(excess, SeekOrigin.Current);
				//var dummy = reader.ReadBytes(excess);
			}
		}
	}
}
