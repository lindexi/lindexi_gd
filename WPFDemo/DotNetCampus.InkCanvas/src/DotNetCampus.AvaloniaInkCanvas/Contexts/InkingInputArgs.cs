using DotNetCampus.Inking.Primitive;

namespace DotNetCampus.Inking.Contexts;

public readonly record struct InkingInputArgs(int Id, InkStylusPoint Point);