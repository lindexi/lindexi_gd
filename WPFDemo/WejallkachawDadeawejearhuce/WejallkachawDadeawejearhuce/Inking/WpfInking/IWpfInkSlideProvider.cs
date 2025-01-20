namespace WejallkachawDadeawejearhuce.Inking.WpfInking;

public interface IWpfInkSlideProvider
{
    void Goto(IWpfInkSlideInfo slideInfo);
    void Remove(IWpfInkSlideInfo slideInfo);
    IWpfInkSlideInfo Create();
}