namespace WejallkachawDadeawejearhuce.Inking.WpfInking;

public interface IWpfInkCanvasWindow
{
    void Show();
    void Hide();
    void Close();

    IWpfInkCanvas WpfInkCanvas { get; }
}