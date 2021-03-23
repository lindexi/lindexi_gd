using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LojafeajahaykaWiweyarcerhelralya
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }



    public class ItemsPresenterWithBottomControl : VirtualizingPanel, IScrollInfo
    {
        //public static readonly DependencyProperty ItemsPresenterProperty = DependencyProperty.Register(
        //    "ItemsPresenter", typeof(ItemsPresenter), typeof(ItemsPresenterWithBottomControl), new PropertyMetadata(default(ItemsPresenter)));

        //public ItemsPresenter ItemsPresenter
        //{
        //    get { return (ItemsPresenter)GetValue(ItemsPresenterProperty); }
        //    set { SetValue(ItemsPresenterProperty, value); }
        //}

        public ItemsPresenterWithBottomControl()
        {
            Loaded += ItemsPresenterWithBottomControl_Loaded;
        }

        private void ItemsPresenterWithBottomControl_Loaded(object sender, RoutedEventArgs e)
        {
            var children = Children;
            //ItemsPresenter = new ItemsPresenter();
            //Children.Add(ItemsPresenter);
        }


        public void LineDown()
        {
            
        }

        public void LineLeft()
        {
        }

        public void LineRight()
        {
        }

        public void LineUp()
        {
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        public void MouseWheelDown()
        {
        }

        public void MouseWheelLeft()
        {
        }

        public void MouseWheelRight()
        {
        }

        public void MouseWheelUp()
        {
        }

        public void PageDown()
        {
        }

        public void PageLeft()
        {
        }

        public void PageRight()
        {
        }

        public void PageUp()
        {
        }

        public void SetHorizontalOffset(double offset)
        {
        }

        public void SetVerticalOffset(double offset)
        {
        }

        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }
        public double ExtentHeight { get; }
        public double ExtentWidth { get; }
        public double HorizontalOffset { get; }
        public ScrollViewer ScrollOwner { get; set; }
        public double VerticalOffset { get; }
        public double ViewportHeight { get; }
        public double ViewportWidth { get; }
    }
}
