using System;

namespace Oxage.Wmf
{
	public enum BitCount
	{
		/// <summary>
		/// Bits per pixel is undefined, only for JPEG and PNG
		/// </summary>
		BI_BITCOUNT_0 = 0x0000,
		/// <summary>
		/// Monochrome
		/// </summary>
		BI_BITCOUNT_1 = 0x0001,
		/// <summary>
		/// 16 colors
		/// </summary>
		BI_BITCOUNT_2 = 0x0004,
		/// <summary>
		/// 256 colors
		/// </summary>
		BI_BITCOUNT_3 = 0x0008,
		/// <summary>
		/// 16-bit colors
		/// </summary>
		BI_BITCOUNT_4 = 0x0010,
		/// <summary>
		/// 24-bit colors
		/// </summary>
		BI_BITCOUNT_5 = 0x0018,
		/// <summary>
		/// 24-bit colors
		/// </summary>
		BI_BITCOUNT_6 = 0x0020
	}
}
