// See https://aka.ms/new-console-template for more information
for (int i = 0; i < 200; i++)
{
    var lowerLatin = GetLowerLatin(i);
    Console.WriteLine($"{i} {lowerLatin}");
}

string GetLowerLatin(int num)
{
    const int startAsciiNum = 'a'; //97
    const int aToZCount = 'z' - 'a' + 2;// 这里的 +2 的意思是比所有的英文字符大一，因为 'z' - 'a' 没有包含一个字符，于是加先 +1 然后这里业务需要比所有英文字符大一于是就再加 1 就是如你看到的方法
    int count = num / aToZCount;
    int index = Math.Max(num % aToZCount, 1);

    var word = (char) (startAsciiNum + index - 1);
    return string.Concat(new string(word, count + 1), ".");
}