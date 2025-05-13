namespace NawrernalgarGibehayle;

readonly
    record
    struct
    RawPointerPoint
    (
        int Id,
        double X,
        double Y,
        int RawWidth,
        int RawHeight,
        double PixelWidth,
        double PixelHeight,
        double PhysicalWidth,
        double PhysicalHeight
    );