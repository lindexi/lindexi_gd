using System.IO;

namespace Oxage.Wmf.Records
{
	public class WmfUnknownRecord : WmfBinaryRecord
	{
		public byte[] Data
		{
			get;
			set;
		}

		public override void Read(BinaryReader reader)
		{
			int length = (int)base.RecordSizeBytes - 6; //Size without RecordSize and RecordType field
			if (length > 0)
			{
				this.Data = reader.ReadBytes(length);
			}
		}
	}
}
