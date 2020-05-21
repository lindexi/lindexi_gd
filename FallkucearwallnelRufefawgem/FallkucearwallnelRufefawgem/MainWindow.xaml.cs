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

namespace FallkucearwallnelRufefawgem
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

    /// <summary>
    /// 画布管理器
    /// </summary>
    public class BoardManagerGrid : Grid
    {
        /// <inheritdoc />
        public BoardManagerGrid()
        {
            SetBoardManager(this, this);
        }

        public static readonly DependencyProperty BoardManagerProperty = DependencyProperty.RegisterAttached(
            "BoardManager", typeof(BoardManagerGrid), typeof(BoardManagerGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetBoardManager(DependencyObject element, BoardManagerGrid value)
        {
            element.SetValue(BoardManagerProperty, value);
        }

        public static BoardManagerGrid GetBoardManager(DependencyObject element)
        {
            return (BoardManagerGrid) element.GetValue(BoardManagerProperty);
        }

        /// <inheritdoc />
        protected override void OnInitialized(EventArgs e)
        {
            var boardList = Children.OfType<Board>().ToList();
            if (boardList.Count != 1)
            {
                // 告诉开发者只能添加一个画布
                return;
            }

            CurrentBoard = boardList[0];

            base.OnInitialized(e);
        }


        public Board CurrentBoard { private set; get; }
    }

    public class FooToolBar : Grid
    {
        private Board CurrentBoard { set; get; }

        /// <inheritdoc />
        public FooToolBar()
        {
            Loaded += FooToolBar_Loaded;
        }

        private void FooToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            var boardManager = BoardManagerGrid.GetBoardManager(this);
            // 自动获得画布
            CurrentBoard = boardManager.CurrentBoard;
        }
    }

    /// <summary>
    /// 画布
    /// </summary>
    public class Board : Canvas
    {
    }
}