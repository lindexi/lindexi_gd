// See https://aka.ms/new-console-template for more information

GetLowerLatin(1);

for (int i = 0; i < 200; i++)
{
    var lowerLatin = GetLowerLatin(i);
    Console.WriteLine($"{i} {lowerLatin}");
}

string GetLowerLatin(int num)
{
    const int startAsciiNum = 'a'; //97
    const int aToZCount = 'z' - 'a' + 1;
    int count = num / aToZCount;
    int index = num % aToZCount;

    var word = (char) (startAsciiNum + index);
    return string.Concat(new string(word, count + 1), ".");
}