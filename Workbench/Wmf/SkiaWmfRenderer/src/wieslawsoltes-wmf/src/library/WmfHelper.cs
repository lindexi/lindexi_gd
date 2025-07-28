using System;
using System.Drawing;
using System.Text;
using Oxage.Wmf.Records;

namespace Oxage.Wmf
{
	public class WmfHelper
	{
		public static IBinaryRecord GetRecordByType(RecordType rt)
		{
			var types = typeof(WmfHelper).Assembly.GetTypes();

			foreach (var type in types)
			{
				if (typeof(IBinaryRecord).IsAssignableFrom(type))
				{
					var attribute = Attribute.GetCustomAttribute(type, typeof(WmfRecordAttribute)) as WmfRecordAttribute;
					if (attribute != null && attribute.Type == rt)
					{
						var record = Activator.CreateInstance(type) as IBinaryRecord;
						return record;
					}
				}
			}

			return null;
		}

		public static Encoding GetAnsiEncoding()
		{
			//ANSI Encoding: http://weblogs.asp.net/ahoffman/archive/2004/01/19/60094.aspx
			//Not sure, should be Encoding.Default? Documentation says "ANSI Character Set" but not specifically which code page
			//return Encoding.Default; //Depends on user's system localization settings
			return Encoding.GetEncoding(1252); //Western European code page
		}

		public static WmfDocument GetExampleFromSpecificationDocument()
		{
			var wmf = new WmfDocument();
			wmf.Records.Clear();

			//META_HEADER
			wmf.Records.Add(new WmfHeader()
			{
				Type = (MetafileType)0x0001,
				HeaderSize = 0x0009,
				Version = 0x0300,
				FileSize = 0x36,
				NumberOfObjects = 0x02,
				MaxRecord = 0x0C,
				NumberOfMembers = 0x00
			});

			//META_CREATEPENINDIRECT
			wmf.Records.Add(new WmfCreatePenIndirectRecord()
			{
				Style = (PenStyle)0x0004,
				Width = new Point(0, 0),
				Color = Color.FromArgb(0, 0, 0)
			});

			//META_SELECTOBJECT
			wmf.Records.Add(new WmfSelectObjectRecord()
			{
				ObjectIndex = 0
			});

			//META_CREATEBRUSHINDIRECT
			wmf.Records.Add(new WmfCreateBrushIndirectRecord()
			{
				Style = (BrushStyle)0x0002,
				Color = Color.FromArgb(0xFF, 0x00, 0xFF),
				Hatch = (HatchStyle)0x0004
			});

			//META_SELECTOBJECT
			wmf.Records.Add(new WmfSelectObjectRecord()
			{
				ObjectIndex = 1
			});

			//META_RECTANGLE
			wmf.Records.Add(new WmfRectangleRecord()
			{
				BottomRect = 0x0046,
				RightRect = 0x0096,
				TopRect = 0x0000,
				LeftRect = 0x0000
			});

			//META_TEXTOUT
			wmf.Records.Add(new WmfTextoutRecord()
			{
				StringLength = 0x000C,
				StringValue = "Hello People",
				YStart = 0x000A,
				XStart = 0x000A
			});

			//META_EOF
			wmf.Records.Add(new WmfEndOfFileRecord());

			return wmf;
		}

		/// <summary>
		/// Gets byte array dump as human-readable "byte[n]" or "null" instead of binary data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string DumpByteArray(byte[] data)
		{
			return (data != null ? "byte[" + data.Length + "]" : "null");
		}
	}
}
