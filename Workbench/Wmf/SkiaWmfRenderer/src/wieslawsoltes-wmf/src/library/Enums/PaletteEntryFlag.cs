using System;

namespace Oxage.Wmf
{
	[Flags]
	public enum PaletteEntryFlag
	{
		PC_RESERVED = 0x01,
		PC_EXPLICIT = 0x02,
		PC_NOCOLLAPSE = 0x04
	}
}
