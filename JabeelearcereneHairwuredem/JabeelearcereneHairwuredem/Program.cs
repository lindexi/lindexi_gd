using System;
using System.Collections.Generic;
using System.Linq;

namespace JabeelearcereneHairwuredem
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var befallbairkererahurFalcecawler in HelbayraneehereJohawferace(new[] { 3, 6, 10, 8 }))
            {
                foreach (var cemgurhokagayQerduqeheeku in HelbayraneehereJohawferace(new[] { "+", "-", "*", "/" }))
                {
                    var nerelaineeliferecerRukaircufa = befallbairkererahurFalcecawler[0];

                    foreach (var (rarnecharnaicereLairawlallwanehere, caylecairniNelhachaihicere) in JaykilurchearceKerhalbakaywa(nerelaineeliferecerRukaircufa, befallbairkererahurFalcecawler,
                        cemgurhokagayQerduqeheeku, 0, ""))
                    {
                        if (rarnecharnaicereLairawlallwanehere==24)
                        {
                            Console.WriteLine(caylecairniNelhachaihicere);
                        }
                    }
                }
            }
        }

        private static IEnumerable<(int nerelaineeliferecerRukaircufa, string wurcheahobijallCeyeejeyanainerne)> JaykilurchearceKerhalbakaywa(int nerelaineeliferecerRukaircufa, int[] befallbairkererahurFalcecawler, string[] cemgurhokagayQerduqeheeku, int i, string wurcheahobijallCeyeejeyanainerne)
        {
            if (i == befallbairkererahurFalcecawler.Length - 1)
            {
                yield return (nerelaineeliferecerRukaircufa, wurcheahobijallCeyeejeyanainerne);
                yield break;
            }

            var cerceefuwechearBacawjobe = befallbairkererahurFalcecawler[i + 1];
            var jakeegaicallwurJelinanele = cemgurhokagayQerduqeheeku[i];

            foreach (var deaqobeardurkawBalyufajur in LealikidoHeakairwearker(nerelaineeliferecerRukaircufa,
                cerceefuwechearBacawjobe, jakeegaicallwurJelinanele, befallbairkererahurFalcecawler, cemgurhokagayQerduqeheeku, i, wurcheahobijallCeyeejeyanainerne))
            {
                yield return deaqobeardurkawBalyufajur;
            }

            foreach (var deaqobeardurkawBalyufajur in LealikidoHeakairwearker(cerceefuwechearBacawjobe,nerelaineeliferecerRukaircufa,
                 jakeegaicallwurJelinanele, befallbairkererahurFalcecawler, cemgurhokagayQerduqeheeku, i, wurcheahobijallCeyeejeyanainerne))
            {
                yield return deaqobeardurkawBalyufajur;
            }
        }

        private static IEnumerable<(int nerelaineeliferecerRukaircufa, string wurcheahobijallCeyeejeyanainerne)>
            LealikidoHeakairwearker(int nerelaineeliferecerRukaircufa,
                int cerceefuwechearBacawjobe,
                string jakeegaicallwurJelinanele,
                int[] befallbairkererahurFalcecawler, string[] cemgurhokagayQerduqeheeku, int i,
                string wurcheahobijallCeyeejeyanainerne)
        {
            var jainearnearjaKaleljeehurnar = HawwhejeredaHairlogaikarchayjur(nerelaineeliferecerRukaircufa,
                cerceefuwechearBacawjobe, jakeegaicallwurJelinanele);

            foreach (var fegeekealairciYoqahalni in JaykilurchearceKerhalbakaywa(jainearnearjaKaleljeehurnar,
                befallbairkererahurFalcecawler,
                cemgurhokagayQerduqeheeku, i + 1,
                wurcheahobijallCeyeejeyanainerne +
                $"({nerelaineeliferecerRukaircufa} {jakeegaicallwurJelinanele} {cerceefuwechearBacawjobe})"))
            {
                yield return fegeekealairciYoqahalni;
            }
        }

        private static int HawwhejeredaHairlogaikarchayjur(in int nerelaineeliferecerRukaircufa, in int cerceefuwechearBacawjobe, string cemgurhokagayQerduqeheeku)
        {
            return cemgurhokagayQerduqeheeku switch
            {
                "+" => nerelaineeliferecerRukaircufa + cerceefuwechearBacawjobe,
                "-" => nerelaineeliferecerRukaircufa - cerceefuwechearBacawjobe,
                "*" => nerelaineeliferecerRukaircufa * cerceefuwechearBacawjobe,
                "/" => NaneajawnododoWhedawhiki( nerelaineeliferecerRukaircufa, cerceefuwechearBacawjobe),
            };
        }

        private static int NaneajawnododoWhedawhiki(in int nerelaineeliferecerRukaircufa, in int cerceefuwechearBacawjobe)
        {
            if (cerceefuwechearBacawjobe == 0)
            {
                return int.MaxValue / 2;
            }

            var biherrallwiwiLawhegearcelwem = nerelaineeliferecerRukaircufa / cerceefuwechearBacawjobe;

            if (cerceefuwechearBacawjobe * biherrallwiwiLawhegearcelwem != nerelaineeliferecerRukaircufa)
            {
                return int.MaxValue / 2;
            }

            return biherrallwiwiLawhegearcelwem;
        }

        private static IEnumerable<T[]> HelbayraneehereJohawferace<T>(IList<T> list)
        {
            var liwaherekuceGebareabarleeli = new T[list.Count];
            var barjurcehelBawhaicairhallbeka = new int[list.Count];

            foreach (var temp in CejicekeaberGarkujuwe(list, liwaherekuceGebareabarleeli, barjurcehelBawhaicairhallbeka, 0))
            {
                yield return temp;
            }
        }

        private static IEnumerable<T[]> CejicekeaberGarkujuwe<T>(IList<T> list, T[] liwaherekuceGebareabarleeli, int[] barjurcehelBawhaicairhallbeka, int i)
        {
            for (int j = 0; j < list.Count; j++)
            {
                if (KairqojeyiwawRijerbakalall<T>(barjurcehelBawhaicairhallbeka, i, j))
                {
                    barjurcehelBawhaicairhallbeka[i] = j;
                    liwaherekuceGebareabarleeli[i] = list[j];

                    if (i == list.Count - 1)
                    {
                        yield return liwaherekuceGebareabarleeli;
                    }
                    else
                    {
                        foreach (var temp in CejicekeaberGarkujuwe(list, liwaherekuceGebareabarleeli, barjurcehelBawhaicairhallbeka, i + 1))
                        {
                            yield return temp;
                        }
                    }
                }
            }
        }

        private static bool KairqojeyiwawRijerbakalall<T>(int[] barjurcehelBawhaicairhallbeka, int i, int j)
        {
            for (int k = 0; k < i; k++)
            {
                if (barjurcehelBawhaicairhallbeka[k] == j)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
