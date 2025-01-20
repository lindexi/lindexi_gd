using WejallkachawDadeawejearhuce.Inking.Contexts;

namespace WejallkachawDadeawejearhuce.Inking.WpfInking;

public interface IWpfInkCanvas
{
    void WritingDown(InkingInputArgs inkingInputArgs);
    void WritingMove(InkingInputArgs inkingInputArgs);
    void WritingUp(InkingInputArgs inkingInputArgs);
}