using System;

namespace Oxage.Wmf
{
	public enum PostScriptCap
	{
		/// <summary>
		/// Do NOT use this value
		/// </summary>
		PostScriptGdiCap = -1,
		/// <summary>
		/// Squared ends of a line.
		/// </summary>
		PostscriptFlatCap = 0,
		/// <summary>
		/// Circular ends of a line.
		/// </summary>
		PostScriptRoundCap = 1,
		/// <summary>
		/// Squared ends of a line where center of the square is the same as line point.
		/// </summary>
		PostScriptSquareCap = 2
	}
}
