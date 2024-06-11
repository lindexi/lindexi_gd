namespace RalllawfairlekolairHemyiqearkice;

class CornerRadiusRectangleEraserViewManager(CornerRadiusRectangleEraserView CornerRadiusRectangleEraserView)
{
    public void MoveEraserVisual(in EraserTouchEventArgs eraserTouchEventArgs)
    {
        CornerRadiusRectangleEraserView.X = eraserTouchEventArgs.X - CornerRadiusRectangleEraserView.Width / 2;
        CornerRadiusRectangleEraserView.Y = eraserTouchEventArgs.Y - CornerRadiusRectangleEraserView.Height / 2;
    }
}
