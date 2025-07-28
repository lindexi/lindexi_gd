using System.IO;

namespace Oxage.Wmf
{
	public interface IBinaryObject
	{
		int GetSize();
		void Read(BinaryReader reader);
		void Write(BinaryWriter writer);
		string Dump();
	}
}
