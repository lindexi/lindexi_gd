using System;

namespace MS.Internal;

[Flags]
internal enum MatrixTypes
{
    TRANSFORM_IS_IDENTITY = 0,
    TRANSFORM_IS_TRANSLATION = 1,
    TRANSFORM_IS_SCALING     = 2,
    TRANSFORM_IS_UNKNOWN     = 4
}