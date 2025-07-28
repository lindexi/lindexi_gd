using System;

namespace Oxage.Wmf
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class WmfRecordAttribute : Attribute
	{
		public WmfRecordAttribute()
		{
		}

		/// <summary>
		/// Gets or sets a record type (aka record function)
		/// </summary>
		public RecordType Type
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets record size in WORDs (number of 16-bit segments)
		/// </summary>
		public uint Size
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
