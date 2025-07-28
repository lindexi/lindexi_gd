using System;

namespace Oxage.Wmf
{
	public enum MixMode
	{
		/// <summary>
		/// Transparent background, no color fill.
		/// </summary>
		TRANSPARENT = 0x0001,
		/// <summary>
		/// Solid color background filled before the text, pen, etc.
		/// </summary>
		OPAQUE = 0x0002
	}
}
