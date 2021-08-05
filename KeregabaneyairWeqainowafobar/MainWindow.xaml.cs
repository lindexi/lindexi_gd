using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeregabaneyairWeqainowafobar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            CursorInfoList = new List<CursorInfo>()
            {
                new CursorInfo(Cursors.AppStarting),
                new CursorInfo(Cursors.Arrow),
                new CursorInfo(Cursors.ArrowCD),
                new CursorInfo(Cursors.Cross),
                new CursorInfo(Cursors.Hand),
                new CursorInfo(Cursors.Help),
                new CursorInfo(Cursors.IBeam),
                new CursorInfo(Cursors.No),
                new CursorInfo(Cursors.None),
                new CursorInfo(Cursors.Pen),
                new CursorInfo(Cursors.ScrollAll),
                new CursorInfo(Cursors.ScrollE),
                new CursorInfo(Cursors.ScrollN),
                new CursorInfo(Cursors.ScrollNE),
                new CursorInfo(Cursors.ScrollNS),
                new CursorInfo(Cursors.ScrollNW),
                new CursorInfo(Cursors.ScrollS),
                new CursorInfo(Cursors.ScrollSE),
                new CursorInfo(Cursors.ScrollSW),
                new CursorInfo(Cursors.ScrollW),
                new CursorInfo(Cursors.ScrollWE),
                new CursorInfo(Cursors.SizeAll),
                new CursorInfo(Cursors.SizeNESW),
                new CursorInfo(Cursors.SizeNS),
                new CursorInfo(Cursors.SizeNWSE),
                new CursorInfo(Cursors.SizeWE),
                new CursorInfo(Cursors.UpArrow),
                new CursorInfo(Cursors.Wait),
            };

            DataContext = this;

            InitializeComponent();
        }

        public List<CursorInfo> CursorInfoList { get; }

        public CursorInfo CurrentCursor
        {
            set
            {
                _currentCursor = value;
                Cursor = value.Cursor;
            }
            get => _currentCursor;
        }

        private CursorInfo _currentCursor;
    }

    public class CursorInfo
    {
        public CursorInfo(Cursor cursor)
        {
            Name = cursor.ToString();
            Cursor = cursor;
        }

        public string Name { get; }

        public Cursor Cursor { get; }
    }
}
