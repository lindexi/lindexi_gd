using System.IO;

namespace Oxage.Wmf
{
	public interface IBinaryRecord
	{
		void Read(BinaryReader reader);
		void Write(BinaryWriter writer);
		string Dump();
	}
}
