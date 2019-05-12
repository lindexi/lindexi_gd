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

namespace DernijacallqaNaycerejerlal
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TouchDown += MainWindow_TouchDown;

            MouseDown += MainWindow_MouseDown;

            Dispatcher.InvokeAsync(async () =>
            {
                var burnerkadelWallnadarli = new BurnerkadelWallnadarli(1, this);
                //while (true)
                {
                    await Task.Delay(1000);
                    burnerkadelWallnadarli.Down();

                    await Task.Delay(1000);
                    burnerkadelWallnadarli.Move();
                }
            });
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            Console.WriteLine("按下");
        }
    }

    public class BurnerkadelWallnadarli : TouchDevice
    {
        /// <inheritdoc />
        public BurnerkadelWallnadarli(int deviceId, Window window) : base(deviceId)
        {
            Window = window;
        }

        /// <summary>
        /// 触摸点
        /// </summary>
        public Point Position { set; get; }

        /// <summary>
        /// 触摸大小
        /// </summary>
        public Size Size { set; get; }

        public void Down()
        {
            TouchAction = TouchAction.Down;

            if (!IsActive)
            {
                SetActiveSource(PresentationSource.FromVisual(Window));

                Activate();
                ReportDown();
            }
            else
            {
                ReportDown();
            }
        }

        public void Move()
        {
            TouchAction = TouchAction.Move;

            ReportMove();
        }

        public void Up()
        {
            TouchAction = TouchAction.Up;

            ReportUp();
            Deactivate();
        }


        private Window Window { get; }

        private TouchAction TouchAction { set; get; }

        /// <inheritdoc />
        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            return new TouchPoint(this, Position, new Rect(Position, Size), TouchAction);
        }

        /// <inheritdoc />
        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            return new TouchPointCollection()
            {
                GetTouchPoint(relativeTo)
            };
        }
    }
}