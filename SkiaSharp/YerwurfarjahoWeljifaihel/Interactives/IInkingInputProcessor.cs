namespace SkiaInkCore.Interactives
{
    /// <summary>
    /// 输入处理者
    /// </summary>
    interface IInkingInputProcessor
    {
        /// <summary>
        /// 是否有效，是否接受输入
        /// </summary>
        bool Enable { get; }

        InkingInputProcessorSettings InputProcessorSettings => InkingInputProcessorSettings.Default;

        /// <summary>
        /// 输入开始
        /// </summary>
        void InputStart();

        /// <summary>
        /// 输入按下
        /// </summary>
        /// <param name="args"></param>
        void Down(InkingModeInputArgs args);

        /// <summary>
        /// 输入移动(此时为按下过程的移动)
        /// </summary>
        /// <param name="args"></param>
        void Move(InkingModeInputArgs args);

        /// <summary>
        /// 输入悬停，即鼠标移动到某个区域但没按下
        /// </summary>
        /// <param name="args"></param>
        void Hover(InkingModeInputArgs args);

        /// <summary>
        /// 输入抬起
        /// </summary>
        /// <param name="args"></param>
        void Up(InkingModeInputArgs args);

        void Leave();

        /// <summary>
        /// 输入完成
        /// </summary>
        void InputComplete();
    }
}