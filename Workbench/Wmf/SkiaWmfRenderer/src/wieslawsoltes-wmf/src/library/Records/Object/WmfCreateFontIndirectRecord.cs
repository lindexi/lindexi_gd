using System.Drawing;
using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_CREATEFONTINDIRECT, SizeIsVariable = true)]
	public class WmfCreateFontIndirectRecord : WmfBinaryRecord
	{
		public WmfCreateFontIndirectRecord() : base()
		{
			this.Height = -48;
			this.Weight = 400;
			this.CharSet = CharacterSet.ANSI_CHARSET;
			this.OutPrecision = OutPrecision.OUT_TT_ONLY_PRECIS;
			this.ClipPrecision = ClipPrecision.CLIP_STROKE_PRECIS;
			this.Quality = FontQuality.DEFAULT_QUALITY;
			this.Pitch = PitchFont.DEFAULT_PITCH;
			this.Family = FamilyFont.FF_DONTCARE;
		}

		public short Height
		{
			get;
			set;
		}

		public short Width
		{
			get;
			set;
		}

		public short Escapement
		{
			get;
			set;
		}

		public short Orientation
		{
			get;
			set;
		}

		public short Weight
		{
			get;
			set;
		}

		public bool Italic
		{
			get;
			set;
		}

		public bool Underline
		{
			get;
			set;
		}

		public bool StrikeOut
		{
			get;
			set;
		}

		public CharacterSet CharSet
		{
			get;
			set;
		}

		public OutPrecision OutPrecision
		{
			get;
			set;
		}

		public ClipPrecision ClipPrecision
		{
			get;
			set;
		}

		public FontQuality Quality
		{
			get;
			set;
		}

		public PitchFont Pitch
		{
			get;
			set;
		}

		public FamilyFont Family
		{
			get;
			set;
		}

		public string FaceName
		{
			get;
			set;
		}

		protected Encoding AnsiEncoding
		{
			get
			{
				return WmfHelper.GetAnsiEncoding();
			}
		}

		public override void Read(BinaryReader reader)
		{
			//Read parameters
			this.Height = reader.ReadInt16();
			this.Width = reader.ReadInt16();
			this.Escapement = reader.ReadInt16();
			this.Orientation = reader.ReadInt16();
			this.Weight = reader.ReadInt16();
			this.Italic = (reader.ReadByte() == 0x01);
			this.Underline = (reader.ReadByte() == 0x01);
			this.StrikeOut = (reader.ReadByte() == 0x01);
			this.CharSet = (CharacterSet)reader.ReadByte();
			this.OutPrecision = (OutPrecision)reader.ReadByte();
			this.ClipPrecision = (ClipPrecision)reader.ReadByte();
			this.Quality = (FontQuality)reader.ReadByte();

			//Read pitch and family
			byte b = reader.ReadByte();
			//TODO: Process pitch and family

			//Read font typeface name
			this.FaceName = null;
			int left = (int)base.RecordSizeBytes - 6 /* RecordSize and RecordFunction */ - 18 /* from Height to PitchAndFamily */;
			if (left > 0)
			{
				byte[] buffer = reader.ReadBytes(left);
				this.FaceName = AnsiEncoding.GetString(buffer);
			}
		}

		public override void Write(BinaryWriter writer)
		{
			//Get font typeface name as byte array of ANSI chars
			byte[] ansi = AnsiEncoding.GetBytes(this.FaceName ?? "");
			if (ansi.Length > 32)
			{
				throw new WmfException("Font typeface name must not exceed 32 chars including the null-terminated char.");
			}

			//Calculate record length
			int length = 4 /* RecordSize */ + 2 /* RecordFunction */
				+ 2 /* Height */ + 2 /* Width */
				+ 2 /* Escapement */ + 2 /* Orientation */
				+ 2 /* Weight */ + 1 /* Italic */ + 1 /* Underline */
				+ 1 /* StrikeOut */ + 1 /* CharSet */ + 1 /* OutPrecision */ + 1 /* ClipPrecision */
				+ 1 /* Quality */ + 1 /* PitchAndFamily */ + ansi.Length /* Facename */;

			int padding = length % 2 == 1 ? 1 : 0;
			int words = length / 2 + padding;
			base.RecordSize = (uint)words;

			//Write length and function
			base.Write(writer);

			//Write initial parameters
			writer.Write(this.Height);
			writer.Write(this.Width);
			writer.Write(this.Escapement);
			writer.Write(this.Orientation);
			writer.Write(this.Weight);
			writer.Write((byte)(this.Italic ? 0x01 : 0x00));
			writer.Write((byte)(this.Underline ? 0x01 : 0x00));
			writer.Write((byte)(this.StrikeOut ? 0x01 : 0x00));
			writer.Write((byte)this.CharSet);
			writer.Write((byte)this.OutPrecision);
			writer.Write((byte)this.ClipPrecision);
			writer.Write((byte)this.Quality);

			//Write pitch and family - two parameters combined in one byte
			byte p = (byte)this.Pitch;
			byte f = (byte)this.Family;
			byte b = (byte)(((f << 4) & 0xF0) | (p & 0x0F)); //TODO: Check, not sure if 'f' and 'p' are in right order, should be swapped?
			//byte b = (byte)(((p << 4) & 0xF0) | (f & 0x0F)); //Uncomment this if line above does not work
			writer.Write(b);

			//Write font typeface name
			writer.Write(ansi);

			//Write empty bytes for padding - align to 16-bit word boundary
			if (padding > 0)
			{
				writer.Write(new byte[padding]);
			}
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			builder.AppendLine("Height: " + this.Height);
			builder.AppendLine("Width: " + this.Width);
			builder.AppendLine("Escapement: " + this.Escapement);
			builder.AppendLine("Orientation: " + this.Orientation);
			builder.AppendLine("Weight: " + this.Weight);
			builder.AppendLine("Italic: " + this.Italic);
			builder.AppendLine("Underline: " + this.Underline);
			builder.AppendLine("StrikeOut: " + this.StrikeOut);
			builder.AppendFormat("CharSet: 0x{0:x4} (CharacterSet.{1})", (byte)this.CharSet, this.CharSet.ToString()).AppendLine();
			builder.AppendFormat("OutPrecision: 0x{0:x2} (OutPrecision.{1})", (byte)this.OutPrecision, this.OutPrecision.ToString()).AppendLine();
			builder.AppendFormat("ClipPrecision: 0x{0:x2} (ClipPrecision.{1})", (byte)this.ClipPrecision, this.ClipPrecision.ToString()).AppendLine();
			builder.AppendFormat("Quality: 0x{0:x2} (FontQuality.{1})", (byte)this.Quality, this.Quality.ToString()).AppendLine();
			builder.AppendFormat("Pitch: 0x{0:x2} (PitchFont.{1})", (byte)this.Pitch, this.Pitch.ToString()).AppendLine();
			builder.AppendFormat("Family: 0x{0:x2} (FamilyFont.{1})", (byte)this.Family, this.Family.ToString()).AppendLine();
			builder.AppendLine("Facename: " + this.FaceName);
		}
	}
}
