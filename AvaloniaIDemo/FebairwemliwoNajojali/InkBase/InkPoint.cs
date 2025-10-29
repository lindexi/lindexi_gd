using NarjejerechowainoBuwurjofear.Inking.Contexts;

namespace InkBase;

public readonly record struct InkPoint(InkId Id, double X, double Y, float PressureFactor = 0.5f);