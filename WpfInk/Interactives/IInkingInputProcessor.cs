namespace WpfInk
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

        void InputStart();

        void Down(InkingModeInputArgs args);

        void Move(InkingModeInputArgs args);

        void Hover(InkingModeInputArgs args);

        void Up(InkingModeInputArgs args);

        void Leave();

        void InputComplete();
    }
}