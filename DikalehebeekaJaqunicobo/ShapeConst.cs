using System.Runtime.CompilerServices;

namespace CPF.Linux;

public static class ShapeConst
{
    /*
       #define SHAPENAME "SHAPE"

       #define SHAPE_MAJOR_VERSION     1       /* current version numbers * /
       #define SHAPE_MINOR_VERSION     1

       #define ShapeSet                        0
       #define ShapeUnion                      1
       #define ShapeIntersect                  2
       #define ShapeSubtract                   3
       #define ShapeInvert                     4

       #define ShapeBounding                   0
       #define ShapeClip                       1
       #define ShapeInput                      2

       #define ShapeNotifyMask                 (1L << 0)
       #define ShapeNotify                     0

       #define ShapeNumberEvents               (ShapeNotify + 1)
     */
    public const string ShapeName = "SHAPE";
    public const string SHAPENAME = ShapeName;

    public const int SHAPE_MAJOR_VERSION = 1; /* current version numbers */
    public const int SHAPE_MINOR_VERSION = 1;

    public const int ShapeSet = 0;
    public const int ShapeUnion = 1;
    public const int ShapeIntersection = 2;
    public const int ShapeSubtract = 3;
    public const int ShapeInvert = 4;

    public const int ShapeBounding = 0;
    public const int ShapeClip = 1;
    public const int ShapeInput = 2;


    public const int ShapeNotifyMask = (1 << 0);
    public const int ShapeNotify = 0;


    public const int ShapeNumberEvents = ShapeNotify + 1;
}