#nullable enable
using Oxage.Wmf.Primitive;
using Oxage.Wmf.Records;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Color = Oxage.Wmf.Primitive.WmfColor;
using Point = Oxage.Wmf.Primitive.WmfPoint;

namespace Oxage.Wmf
{
	public partial class WmfHelper
	{
		public static IBinaryRecord? GetRecordByType(RecordType rt)
		{
            if (RecordCreatorDictionary.TryGetValue(rt,out var creator))
            {
                return creator.Creator();
            }

			return null;
		}

        private static Dictionary<RecordType, RecordCreator> RecordCreatorDictionary
        {
            get
            {
                if (_recordCreatorDictionary is null)
                {
                    var dictionary = new Dictionary<RecordType, RecordCreator>();
                    foreach (var (type, attribute, creator) in ExportBinaryRecordEnumerable())
                    {
                        dictionary.Add(attribute.Type, new RecordCreator(type, attribute, creator));
                    }
                    _recordCreatorDictionary = dictionary;
                }

                return _recordCreatorDictionary;
            }
        }
        
        private static Dictionary<RecordType, RecordCreator>? _recordCreatorDictionary;

        readonly record struct RecordCreator(Type Type, WmfRecordAttribute Attribute, Func<IBinaryRecord> Creator);

        /// <summary>
        /// This method is used to export all binary records that are defined in the library. PowerBy Telescope, the source generator.
        /// </summary>
        /// <returns></returns>
        [dotnetCampus.Telescope.TelescopeExportAttribute()]
        private static partial IEnumerable<(Type type, WmfRecordAttribute attribute, Func<IBinaryRecord> creator)> ExportBinaryRecordEnumerable();

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
				TextByteArray = WmfHelper.GetAnsiEncoding().GetBytes("Hello People"),
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
