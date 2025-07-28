using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Oxage.Wmf.Objects
{
	public abstract class WmfBinaryObject : IBinaryObject
	{
		#region IBinaryRecord Members

		/// <summary>
		/// Gets the size of the object in bytes. Important: variable size objects should override this method.
		/// </summary>
		/// <returns>Returns size of the object in bytes.</returns>
		public virtual int GetSize()
		{
			var attribute = Attribute.GetCustomAttributes(this.GetType()).FirstOrDefault(x => x is WmfObjectAttribute) as WmfObjectAttribute;
			if (attribute != null)
			{
				if (attribute.SizeIsVariable)
				{
					//Variable size object - should be overridden
					throw new NotImplementedException();
				}
				else
				{
					//Fixed size object
					return attribute.Size;
				}
			}

			throw new WmfException("Cannot get size of the object [" + this.GetType().ToString() + "], missing WmfObject attribute.");
		}

		public abstract void Read(BinaryReader reader);

		public abstract void Write(BinaryWriter writer);

		public string Dump()
		{
			var builder = new StringBuilder();
			Dump(builder);
			return builder.ToString();
		}

		#endregion

		public virtual void Dump(StringBuilder builder)
		{
			builder.AppendFormat("\t== {0} ==", this.GetType().Name).AppendLine();
			//Other fields should be added in overridden method
		}
	}
}
