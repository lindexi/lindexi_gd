using System.IO;
using System.Text;

namespace Oxage.Wmf.Records
{
	public class WmfFormat : IBinaryRecord
	{
		#region Public properties
		public ushort Handle
		{
			get;
			set;
		}

		public ushort Left
		{
			get;
			set;
		}

		public ushort Top
		{
			get;
			set;
		}

		public ushort Right
		{
			get;
			set;
		}

		public ushort Bottom
		{
			get;
			set;
		}

		public ushort Unit
		{
			get;
			set;
		}

		public ushort Checksum
		{
			get;
			set;
		}
		#endregion

		#region Public methods
		public WmfFormat Clone()
		{
			return new WmfFormat()
			{
				Handle = this.Handle,
				Left = this.Left,
				Top = this.Top,
				Right = this.Right,
				Bottom = this.Bottom,
				Unit = this.Unit,
				Checksum = this.Checksum
			};
		}

		public ushort CalculateChecksum()
		{
			ushort sum = 0;
			using (var stream = new MemoryStream())
			{
				using (var writer = new BinaryWriter(stream))
				{
					//Get record without checksum
					var clone = Clone();
					clone.Checksum = 0;
					clone.Write(writer);

					//Convert record to byte array
					byte[] data = stream.ToArray();

					//Use BinaryReader to read word by word (word = ushort = 16 bit)
					using (var reader = new BinaryReader(new MemoryStream(data)))
					{
						//Calculate checksum from the first 10 words
						for (int i = 0; i < 10; i++)
						{
							ushort word = reader.ReadUInt16();
							sum ^= word;
						}
					}
				}
			}
			return sum;
		}
		#endregion

		#region Public methods
		public override string ToString()
		{
			return Dump();
		}
		#endregion

		#region IBinaryRecord Members
		public void Read(BinaryReader reader)
		{
			byte[] key = reader.ReadBytes(4);
			if (key[0] != 0xD7 || key[1] != 0xCD || key[2] != 0xC6 || key[3] != 0x9A)
			{
				throw new WmfException("WMF key does not match the pattern!");
			}

			this.Handle = reader.ReadUInt16();
			this.Left = reader.ReadUInt16();
			this.Top = reader.ReadUInt16();
			this.Right = reader.ReadUInt16();
			this.Bottom = reader.ReadUInt16();
			this.Unit = reader.ReadUInt16();
			uint reserved = reader.ReadUInt32();
			this.Checksum = reader.ReadUInt16();
		}

		public void Write(BinaryWriter writer)
		{
			writer.Write(new byte[] { 0xD7, 0xCD, 0xC6, 0x9A }); //Key
			writer.Write(this.Handle);
			writer.Write(this.Left);
			writer.Write(this.Top);
			writer.Write(this.Right);
			writer.Write(this.Bottom);
			writer.Write(this.Unit);
			writer.Write((uint)0); //Reserved
			writer.Write(this.Checksum);
		}

		public string Dump()
		{
			var builder = new StringBuilder();
			builder.AppendLine("== WmfFormat ==");
			builder.AppendLine("Handle: " + this.Handle);
			builder.AppendLine("Left: " + this.Left);
			builder.AppendLine("Top: " + this.Top);
			builder.AppendLine("Right: " + this.Right);
			builder.AppendLine("Bottom: " + this.Bottom);
			builder.AppendLine("Unit: " + this.Unit);
			builder.AppendLine("Checksum: " + this.Checksum);
			return builder.ToString();
		}
		#endregion
	}
}
