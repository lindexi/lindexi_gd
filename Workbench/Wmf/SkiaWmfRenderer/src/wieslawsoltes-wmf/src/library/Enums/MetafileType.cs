using System;

namespace Oxage.Wmf
{
	public enum MetafileType
	{
		/// <summary>
		/// Metafile is stored in memory.
		/// </summary>
		MEMORYMETAFILE = 0x0001,
		/// <summary>
		/// Metafile is stored on disk.
		/// </summary>
		DISKMETAFILE = 0x0002
	}
}
