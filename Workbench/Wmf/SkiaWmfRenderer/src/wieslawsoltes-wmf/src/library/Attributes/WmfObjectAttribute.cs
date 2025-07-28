using System;

namespace Oxage.Wmf
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class WmfObjectAttribute : Attribute
	{
		public WmfObjectAttribute()
		{
		}

		/// <summary>
		/// Gets or sets objects size in WORDs (number of 16-bit segments)
		/// </summary>
		public int Size
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets if the size is defined as variable (false by default)
		/// </summary>
		public bool SizeIsVariable
		{
			get;
			set;
		}
	}
}
