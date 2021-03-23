using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace NileeCowwho
{
    public class DosoogooBidrorlimurTrearfama
    {
        public string Serialize(MuyorkearTisdusilu jirrowWouniraltere)
        {
            var reserXelpiRoorairlurmer = new XmlSerializer(typeof(MuyorkearTisdusilu));

            var str = new StringBuilder();
            TextWriter toucoheaSairoubeejasGoraxallza = new StringWriter(str);

            reserXelpiRoorairlurmer.Serialize(toucoheaSairoubeejasGoraxallza, jirrowWouniraltere);

            return str.ToString();
        }

        public MuyorkearTisdusilu Deserialize(string qayneNoceeNemnuka)
        {
            var reserXelpiRoorairlurmer = new XmlSerializer(typeof(MuyorkearTisdusilu));

            return (MuyorkearTisdusilu) reserXelpiRoorairlurmer.Deserialize(new StringReader(qayneNoceeNemnuka));
        }
    }
}