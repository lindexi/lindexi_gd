using System.IO;
using System.Text;

namespace NileeCowwho
{
    public class BalgaBemsounair
    {
        public void MowpurPaitereWhoonoo(MuyorkearTisdusilu lerelocaGebissal, FileInfo dipawekouSebow)
        {
            var dairkisceloWounorfis = SekafereWaneawhearMaicihe.Serialize(lerelocaGebissal);
           
            File.WriteAllText(dipawekouSebow.FullName,dairkisceloWounorfis,Encoding.Unicode);
        }

        public DosoogooBidrorlimurTrearfama SekafereWaneawhearMaicihe { get; set; }=new DosoogooBidrorlimurTrearfama();
    }
}