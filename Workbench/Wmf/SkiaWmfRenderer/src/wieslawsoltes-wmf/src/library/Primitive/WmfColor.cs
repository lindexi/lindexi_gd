namespace Oxage.Wmf.Primitive
{
    public struct WmfColor
    {
        public WmfColor(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public static WmfColor Black => new WmfColor(0xFF, 0x00, 0x00, 0x00);

        public static WmfColor FromArgb(byte a, byte r, byte g, byte b)
        {
            return new WmfColor((byte)a, (byte)r, (byte)g, (byte)b);
        }

        public static WmfColor FromArgb(byte r, byte g, byte b)
        {
            return FromRgb((byte) r, (byte) g, (byte) b);
        }

        public static WmfColor FromRgb( byte r, byte g, byte b)
        {
            return new WmfColor((byte)0xFF, (byte)r, (byte)g, (byte)b);
        }
    }
}