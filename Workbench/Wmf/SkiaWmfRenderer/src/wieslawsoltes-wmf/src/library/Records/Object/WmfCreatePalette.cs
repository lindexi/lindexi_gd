using System.IO;
using System.Text;
using Oxage.Wmf.Objects;

namespace Oxage.Wmf.Records
{
	[WmfRecord(Type = RecordType.META_CREATEPALETTE, SizeIsVariable = true)]
	public class WmfCreatePalette : WmfBinaryRecord
	{
		public WmfCreatePalette() : base()
		{
		}

		/// <summary>
		/// 1-based index for WmfSelectPalette.Palette
		/// </summary>
		public short PaletteIndex { get; set; }

		public Palette Palette
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			Palette = new Palette(RecordType.META_CREATEPALETTE);
			Palette.Read(reader);
			this.RecordSizeBytes = Palette.SizeBytes;
		}

		public override void Write(BinaryWriter writer)
		{
			base.Write(writer);
			Palette.Write(writer);
		}

		protected override void Dump(StringBuilder builder)
		{
			base.Dump(builder);
			Palette.Dump(builder);
		}
	}
}
