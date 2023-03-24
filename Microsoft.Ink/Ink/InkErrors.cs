// Decompiled with JetBrains decompiler
// Type: Microsoft.Ink.InkErrors
// Assembly: Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: FC37C175-0B5F-4425-BE1C-C6B14BB882AF
// Assembly location: C:\Windows\assembly\GAC_64\Microsoft.Ink\6.1.0.0__31bf3856ad364e35\Microsoft.Ink.dll

using System;

namespace Microsoft.Ink
{
  internal static class InkErrors
  {
    internal const int MismatchedInkObject = -2144862206;
    internal const int Busy = -2144862205;
    internal const int IncompatibleObject = -2144862204;
    internal const int WindowNotSet = -2144862203;
    internal const int InvalidMode = -2144862202;
    internal const int Enabled = -2144862201;
    internal const int NoStrokesToRecognize = -2144862200;
    internal const int EmptyRecognitionResult = -2144862199;
    internal const int InvalidProperty = -2147220927;
    internal const int RecognizerNotRegistered = -2147220939;
    internal const int InvalidDataFromRecognizer = -2147220934;
    internal const int OverlappingInputRect = -2144862192;
    internal const int InvalidStroke = -2147220958;
    internal const int NoDefaultTablet = -2147220974;
    internal const int UnknownProperty = -2147220965;
    internal const int InvalidInputRect = -2147220967;
    internal const int InitializeFail = -2147220957;
    internal const int NotRelevant = -2147220942;
    internal const int InvalidRights = -2147220938;
    internal const int OutOfOrderCall = -2147220937;
    internal const int StylusQueueFull = -2147220936;
    internal const int RtsInvalidConfiguration = -2147220935;

    internal static Exception GetExceptionForInkError(int error)
    {
      switch (error)
      {
        case -2147220974:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("NoDefaultTablet"));
        case -2147220967:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InvalidInputRect"));
        case -2147220965:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("UnknownProperty"));
        case -2147220957:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InitializeFail"));
        case -2147220942:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("NotRelevant"));
        case -2147220939:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RecognizerNotRegistered"));
        case -2147220938:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InvalidRights"));
        case -2147220937:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("OutOfOrderCall"));
        case -2147220936:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("StylusQueueFull"));
        case -2147220935:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("RtsInvalidConfiguration"));
        case -2147220934:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InvalidDataFromRecognizer"));
        case -2147220927:
          return (Exception) new ArgumentException(Helpers.SharedResources.Errors.GetString("InvalidProperty"));
        case -2144862206:
          return (Exception) new ArgumentException(Helpers.SharedResources.Errors.GetString("MismatchedInkObject"));
        case -2144862205:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InkCollectorBusy"));
        case -2144862204:
          return (Exception) new ArgumentException(Helpers.SharedResources.Errors.GetString("IncompatibleObject"));
        case -2144862203:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("WindowNotSet"));
        case -2144862202:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InvalidMode"));
        case -2144862201:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("InkCollectorEnabled"));
        case -2144862200:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("NoStrokesToRecognize"));
        case -2144862199:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("EmptyRecognitionResult"));
        case -2144862192:
          return (Exception) new InvalidOperationException(Helpers.SharedResources.Errors.GetString("OverlappingInputRect"));
        default:
          return (Exception) null;
      }
    }

    internal static void ThrowExceptionForInkError(int error)
    {
      Exception exceptionForInkError = InkErrors.GetExceptionForInkError(error);
      if (exceptionForInkError != null)
        throw exceptionForInkError;
    }
  }
}
