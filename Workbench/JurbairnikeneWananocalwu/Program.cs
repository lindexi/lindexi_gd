// See https://aka.ms/new-console-template for more information

Delegate d = SetText;

string t = "123";
var l = t.Length;

Console.WriteLine("Hello, World!");
Console.ReadLine();

void SetText(string input)
{
    if (t.Length < input.Length)
    {

    }

    t = input;
}

struct F1
{
    public string Text;
}

/*
  private static void <Main>$(string[] args)
   {
     Program.<>c__DisplayClass0_0 cDisplayClass00;
     Program.<<Main>$>g__SetText|0_0("123", ref cDisplayClass00);
     int l = cDisplayClass00.t.Length;
     Console.WriteLine("Hello, World!");
     Console.ReadLine();
   }

   public Program()
   {
     base..ctor();
   }

   [NullableContext(1)]
   [CompilerGenerated]
   internal static void <<Main>$>g__SetText|0_0(string input, [In] ref Program.<>c__DisplayClass0_0 obj1)
   {
     obj1.t = input;
   }

   [CompilerGenerated]
   [StructLayout(LayoutKind.Auto)]
   private struct <>c__DisplayClass0_0
   {
     public string t;
   }
 */