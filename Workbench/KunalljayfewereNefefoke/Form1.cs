using System.Runtime.InteropServices;

namespace KunalljayfewereNefefoke;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        Load += Form1_Load;
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        _ = CopyFromClipboard();
    }

    private async Task<bool> CopyFromClipboard()
    {
        while (Clipboard.ContainsText())
        {
            try
            {
                string text = Clipboard.GetText();
                
            }
            catch (ExternalException)
            {
                await Task.Delay(5).ConfigureAwait(true);
            }
        }

        return false;
    }
}
