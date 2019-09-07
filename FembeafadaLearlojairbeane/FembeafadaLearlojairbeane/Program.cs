using System;
using System.Collections.Generic;
using System.Linq;

namespace FembeafadaLearlojairbeane
{
    class Program
    {
        public static void Cashier()
        {
            // 商品和价格列表
            List<int> priceList = new List<int>()
            {
                74, 64, 74, 33, 82
            };

            var firstLine = Console.ReadLine();
            // 第一行表示输入的行
            int count = int.Parse(firstLine);

            var sum = 0;
            for (int i = 0; i < count; i++)
            {
                var line = Console.ReadLine();
                // 分割空格，如果有人能用正则更好

                var split = line.Split(' ');
                var identifier = int.Parse(split[0]);
                var number = int.Parse(split[1]);

                sum += priceList[identifier - 1] * number;
            }

            Console.WriteLine(sum);
        }

        static void Main(string[] args)
        {
            Cashier();
            return;

            string str = @"冲虚奇鬼棍
                墨羽重阙谱
            摧魂利绸杵
                双重细毛杵
            卷云广岩牌
                慾天幻皇刺
            环纹破缕帕
                黑墨硬土盒
            雾灵古泉杵
                白鹿小目铃
            轻捷明蕊壶
                金涛小铜印
            天广岩尺";

            var list = str.Split('\n').Select(temp => temp.Trim()).ToList();

            var random = new Random();

            for (var i = 0; i < list.Count; i++)
            {
                var temp = list[i];
                Console.WriteLine($@"商品名：{temp}
商品编号：{i + 1}
商品价格：{random.Next(100)}
");
            }
        }
    }
}