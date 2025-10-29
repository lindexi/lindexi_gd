namespace UnoInk.Inking.InkCore.Interactives;

/// <summary>
/// 输入处理者
/// </summary>
public interface IInputProcessor
{
    /// <summary>
    /// 是否有效，是否接受输入
    /// </summary>
    bool Enable { get; }

    InputProcessorSettings InputProcessorSettings => InputProcessorSettings.Default;

    void InputStart();

    void Down(ModeInputArgs args);

    void Move(ModeInputArgs args);

    void Hover(ModeInputArgs args);

    void Up(ModeInputArgs args);

    void Leave();

    void InputComplete();
}