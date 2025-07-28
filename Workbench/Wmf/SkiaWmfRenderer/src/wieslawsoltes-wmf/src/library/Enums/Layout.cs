using System;

namespace Oxage.Wmf
{
	public enum Layout
	{
		/// <summary>
		/// Sets right-to-left as the default horizontal layout.
		/// </summary>
		LAYOUT_RTL = 0x00000001,
		/// <summary>
		/// Sets bottom-to-top as the default horizontal layout.
		/// </summary>
		LAYOUT_BTT = 0x00000002,
		/// <summary>
		/// Sets the default layout to vertical.
		/// </summary>
		LAYOUT_VBH = 0x00000004,
		/// Disables any reflection (META_BITBLT and META_STRETCHBLT operations)
		/// </summary>
		LAYOUT_BITMAPORIENTATIONPRESERVED = 0x00000008
	}
}
