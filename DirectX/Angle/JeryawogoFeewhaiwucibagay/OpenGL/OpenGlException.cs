using System;
using System.Collections.Generic;
using System.Text;

namespace JeryawogoFeewhaiwucibagay.OpenGL;

public class OpenGlException:Exception
{
    public int? ErrorCode { get; }

    public OpenGlException(string? message) : base(message)
    {
    }

    private OpenGlException(string? message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}
