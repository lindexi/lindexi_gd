// See https://aka.ms/new-console-template for more information

int value = 1 << 31;

var text = value.ToString("X");

value = 0xFFFF;

value = value << 16;
text = value.ToString("X");

value = int.MaxValue;
value = value >> 16;
text = value.ToString("X");

;

Console.WriteLine("Hello, World!");
