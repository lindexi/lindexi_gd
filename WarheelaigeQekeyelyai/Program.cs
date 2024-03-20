using Cairo;

using Gdk;

using Gtk;

using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace WarheelaigeQekeyelyai;

internal class Program
{
        [STAThread]
    static void Main(string[] args)
    {
        Application.Init();

        App = new Application("org.Samples.Samples", GLib.ApplicationFlags.None);
        App.Register(GLib.Cancellable.Current);

        Win = new MainWindow("Demo Window");
        App.AddWindow(Win);

        var menu = new GLib.Menu();
        menu.AppendItem(new GLib.MenuItem("Help", "app.help"));
        menu.AppendItem(new GLib.MenuItem("About", "app.about"));
        menu.AppendItem(new GLib.MenuItem("Quit", "app.quit"));
        App.AppMenu = menu;

        var helpAction = new GLib.SimpleAction("help", null);
        App.AddAction(helpAction);

        var aboutAction = new GLib.SimpleAction("about", null);
        App.AddAction(aboutAction);

        var quitAction = new GLib.SimpleAction("quit", null);
        App.AddAction(quitAction);

        Win.ShowAll();
        Application.Run();
    }

    public static Application App;
    public static Window Win;
}

class MainWindow : Window
{
        private HeaderBar _headerBar;
        private TreeView _treeView;
        private Box _boxContent;
        private TreeStore _store; private Dictionary<string, (Type type, Widget widget)> _items;
        //private SourceView _textViewCode;
        private Notebook _notebook; public MainWindow(string title) : base(WindowType.Toplevel)
    {
        WindowPosition = WindowPosition.Center;
        DefaultSize = new Size(600, 600);


        _headerBar = new HeaderBar();
        _headerBar.ShowCloseButton = true;
        _headerBar.Title = "GtkSharp Sample Application";

        var btnClickMe = new Button();
        btnClickMe.AlwaysShowImage = true;
        btnClickMe.Image = Image.NewFromIconName("document-new-symbolic", IconSize.Button);
        _headerBar.PackStart(btnClickMe);

        //Titlebar = _headerBar;

        var hpanned = new HPaned();
        hpanned.Position = 200;

        _treeView = new TreeView();
        _treeView.HeadersVisible = false;
        hpanned.Pack1(_treeView, false, true);

        _notebook = new Notebook();

        var scroll1 = new ScrolledWindow();
        var vpanned = new VPaned();
        vpanned.Position = 300;
        _boxContent = new Box(Orientation.Vertical, 0);
        _boxContent.Margin = 8;
        vpanned.Pack1(_boxContent, true, true);
        //vpanned.Pack2(ApplicationOutput.Widget, false, true);
        scroll1.Child = vpanned;
        _notebook.AppendPage(scroll1, new Label { Text = "Data", Expand = true });

        _notebook.AppendPage(new F(), new Label() { Text = "Code2", Expand = true });

        hpanned.Pack2(_notebook, true, true);

        Child = hpanned;

        //Widget widget = this;
        //this.TouchEvent += MainWindow_TouchEvent;
        //var hpanned = new HPaned();
        //hpanned.Position = 20;
        //var f = new F();
        //hpanned.Pack1(f, false, true);
        //var button = new Button()
        //{

        //};
        //button.Image = Image.NewFromIconName("document-new-symbolic", IconSize.Button);
        //hpanned.Pack2(button,false,true);

        //Child = hpanned;
    }

    private void MainWindow_TouchEvent(object o, TouchEventArgs args)
    {
    }
}

class F : DrawingArea
{
    public F()
    {
        WidthRequest = 300;
        HeightRequest = 300;
    }

    protected override bool OnDrawn(Context cr)
    {
        cr.SetSourceRGB(0.9, 0, 0);
        cr.LineWidth = 10;
        cr.MoveTo(10, 10);
        cr.LineTo(100, 10);
        cr.Stroke();
        return base.OnDrawn(cr);
    }
}