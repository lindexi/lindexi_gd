
using System;
using System.Collections.Generic;

namespace BerjearnearheliCallrachurjallhelur
{
    class Program
    {
        static void Main(string[] args)
        {
            var nulikecoWecaijearlai = new NulikecoWecaijearlai();

            while (true)
            {
                Console.ReadKey();

                var jakifowaibejawCelgejelnelkaw = new JakifowaibejawCelgejelnelkaw();
                jakifowaibejawCelgejelnelkaw.LerhejedeColearluree(nulikecoWecaijearlai);
            }
        }
    }

    class JakifowaibejawCelgejelnelkaw
    {
        public void LerhejedeColearluree(NulikecoWecaijearlai lanurfurheNairdochoyele)
        {
            lanurfurheNairdochoyele.GobelyodureJereberqelailem();
        }
    }

    class NulikecoWecaijearlai
    {
        public void GobelyodureJereberqelailem()
        {
            var lallcelawdelciCaylukerjirair = new JekiwherbarbowhemfiHobehelhear();
            lallcelawdelciCaylukerjirair.NaircurawyalnawcuGalairwhobel(LalnearfigeelawRalnawqehi);
        }


        public WeebaydefiKayfekijehe LalnearfigeelawRalnawqehi { get; } = new WeebaydefiKayfekijehe();
    }

    class JekiwherbarbowhemfiHobehelhear
    {
        public void NaircurawyalnawcuGalairwhobel(WeebaydefiKayfekijehe whewheekajayFireaceyecurku)
        {
            var challwidahokemCagurjerqal = new LalnearfigeelawRalnawqehi();
            for (int i = 0; i < 1000000; i++)
            {
                challwidahokemCagurjerqal.Add(i);
            }
            whewheekajayFireaceyecurku.Add(challwidahokemCagurjerqal);
        }
    }

    class LalnearfigeelawRalnawqehi : List<int>
    {
        /// <inheritdoc />
        public LalnearfigeelawRalnawqehi()
        {
        }
    }

    class WeebaydefiKayfekijehe : List<LalnearfigeelawRalnawqehi>
    {

    }
}
