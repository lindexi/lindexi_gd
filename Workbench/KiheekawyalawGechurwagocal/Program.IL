.class private auto ansi beforefieldinit
  Program
    extends [System.Runtime]System.Object
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor()
    = (01 00 00 00 )

  .method private hidebysig static void
    '<Main>$'(
      string[] args
    ) cil managed
  {
    .entrypoint
    .maxstack 1
    .locals init (
      [0] valuetype KiheekawyalawGechurwagocal.DisposableStruct V_0
    )

    IL_0000: ldloca.s     V_0
    IL_0002: initobj      KiheekawyalawGechurwagocal.DisposableStruct
    .try
    {
      IL_0008: ldstr        "Hello, World!"
      IL_000d: call         void [System.Console]System.Console::WriteLine(string)
      IL_0012: leave.s      IL_0022
    } // end of .try
    finally
    {
      IL_0014: ldloca.s     V_0
      IL_0016: constrained. KiheekawyalawGechurwagocal.DisposableStruct
      IL_001c: callvirt     instance void [System.Runtime]System.IDisposable::Dispose()
      IL_0021: endfinally
    } // end of finally
    IL_0022: ret

  } // end of method Program::'<Main>$'

  .method public hidebysig specialname rtspecialname instance void
    .ctor() cil managed
  {
    .maxstack 8

    IL_0000: ldarg.0      // this
    IL_0001: call         instance void [System.Runtime]System.Object::.ctor()
    IL_0006: ret

  } // end of method Program::.ctor
} // end of class Program