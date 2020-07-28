using dotnetCampus.OpenXMLUnitConverter;

namespace GakagaycalhechemNerehejejairairway
{
    internal class MarginThickness
    {
        public MarginThickness()
        {
        }

        public MarginThickness(Pixel left, Pixel top, Pixel right, Pixel bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public Pixel Left { set; get; }
        public Pixel Top { set; get; }
        public Pixel Right { set; get; }
        public Pixel Bottom { set; get; }
    }
}