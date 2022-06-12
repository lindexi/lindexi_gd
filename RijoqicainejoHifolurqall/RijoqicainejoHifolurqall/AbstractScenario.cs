using Microsoft.Maui.Graphics;

namespace GraphicsTester.Scenarios
{
    public abstract class AbstractScenario : IPicture, IDrawable
    {
        public static readonly float[] SOLID = null;
        public static readonly float[] DOT_DOT = { 1, 1 };
        public static readonly float[] DOTTED = { 2, 2 };
        public static readonly float[] DASHED = { 4, 4 };
        public static readonly float[] LONG_DASHES = { 8, 4 };
        public static readonly float[] EXTRA_LONG_DASHES = { 16, 4 };
        public static readonly float[] DASHED_DOT = { 4, 4, 1, 4 };
        public static readonly float[] DASHED_DOT_DOT = { 4, 4, 1, 4, 1, 4 };
        public static readonly float[] LONG_DASHES_DOT = { 8, 4, 2, 4 };
        public static readonly float[] EXTRA_LONG_DASHES_DOT = { 16, 4, 8, 4 };

        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public AbstractScenario(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public AbstractScenario(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public virtual void Draw(ICanvas canvas)
        {
            // Do nothing by default
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            Draw(canvas);
        }

        public string Hash { get; set; }

        public override string ToString()
        {
            return GetType().Name;
        }

        public IImage ToImage(int width, int height, float scale = 1)
        {
            throw new System.NotImplementedException();
        }
    }
}
