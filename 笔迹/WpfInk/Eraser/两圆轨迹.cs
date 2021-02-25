namespace Eraser
{
    class 两圆轨迹
    {
        public 两圆轨迹(圆 圆1, 圆 圆2, 线段 线段1, 线段 线段2)
        {
            this.圆1 = 圆1;
            this.圆2 = 圆2;
            this.线段1 = 线段1;
            this.线段2 = 线段2;
        }

        public 圆 圆1 { get; }
        public 圆 圆2 { get; }
        public 线段 线段1 { get; }
        public 线段 线段2 { get; }
    }
}