using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChanegaiciwujaLerunearjayfay
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            var timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            timer.Start();

            _icon = NotifyIcon.Icon;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _showIcon = !_showIcon;
            if (_showIcon)
            {
                NotifyIcon.Icon = _icon;
            }
            else
            {
                NotifyIcon.Icon = null;
            }
        }

        private bool _showIcon = true;
        private readonly Icon _icon;
    }
}
