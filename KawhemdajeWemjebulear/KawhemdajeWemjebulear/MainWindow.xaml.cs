using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace KawhemdajeWemjebulear
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            F.BeginInit();
            F.Source = "123";
            F.EndInit();
        }
    }

    public class F : UIElement, ISupportInitialize, ISupportInitializeNotification
    {
        private string _source;

        public string Source
        {
            set
            {
                if (!_beginInit)
                {
                    throw new InvalidOperationException("不能在初始化之后修改");
                }

                _source = value;
            }
        }

        /// <inheritdoc />
        public void BeginInit()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("不能多次初始化");
            }

            _beginInit = true;
        }

        /// <inheritdoc />
        public void EndInit()
        {
            IsInitialized = true;
            _beginInit = false;
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        private bool _beginInit;

        /// <inheritdoc />
        public bool IsInitialized { private set; get; }

        /// <inheritdoc />
        public event EventHandler Initialized;
    }
}