using System;
using System.Xml.Serialization;

namespace NileeCowwho
{
    class Program
    {
        static void Main(string[] args)
        {
            var geceWhiyu = new GeceWhiyu();
            geceWhiyu.PeenorJaidorsayyou();

            var reserXelpiRoorairlurmer = new XmlSerializer(typeof(MuyorkearTisdusilu));

            var muyorkearTisdusilu = new MuyorkearTisdusilu()
            {
               
            };

            var dosoogooBidrorlimurTrearfama = new DosoogooBidrorlimurTrearfama();
            var trayteeTrecabawKayloo = dosoogooBidrorlimurTrearfama.Serialize(muyorkearTisdusilu);
            Console.WriteLine(trayteeTrecabawKayloo);
            dosoogooBidrorlimurTrearfama.Deserialize(trayteeTrecabawKayloo);
        }
    }
}