using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.StylusInput;
using Microsoft.StylusInput.PluginData;

namespace Babukeelleneeoai
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += MainWindow_SourceInitialized;

        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var handle = windowInteropHelper.Handle;

            var realTimeStylus = new RealTimeStylus(handle)
            {
                MultiTouchEnabled = true,
                AllTouchEnabled = true,
            };
            RealTimeStylus = realTimeStylus;

            var styluePlugIn = new StyluePlugIn();
            realTimeStylus.SyncPluginCollection.Add(styluePlugIn);
            // 不能提前，否则提示没有添加任何的 PlugIn 对象
            realTimeStylus.Enabled = true;
        }

        private RealTimeStylus RealTimeStylus { set; get; }

        private void Border_TouchDown(object sender, TouchEventArgs e)
        {

        }

        private void Border_StylusDown(object sender, StylusDownEventArgs e)
        {

        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }
    }

    public class StyluePlugIn : UIElement, IStylusSyncPlugin
    {
        /// <inheritdoc />
        public void RealTimeStylusEnabled(RealTimeStylus sender, RealTimeStylusEnabledData data)
        {

        }

        /// <inheritdoc />
        public void RealTimeStylusDisabled(RealTimeStylus sender, RealTimeStylusDisabledData data)
        {
        }

        /// <inheritdoc />
        void IStylusSyncPlugin.StylusInRange(RealTimeStylus sender, StylusInRangeData data)
        {
        }

        /// <inheritdoc />
        void IStylusSyncPlugin.StylusOutOfRange(RealTimeStylus sender, StylusOutOfRangeData data)
        {
        }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        private StrokeCollection StrokeCollection { get; } = new StrokeCollection();

        private Dictionary<int, StylusPointCollection> StrokeList { get; } = new Dictionary<int, StylusPointCollection>();

        /// <inheritdoc />
        void IStylusSyncPlugin.StylusDown(RealTimeStylus sender, StylusDownData data)
        {
            var stylusPointCollection = new StylusPointCollection();
            StrokeList[data.Stylus.Id] = stylusPointCollection;

            stylusPointCollection.Add(new StylusPoint());

            StrokeCollection.Add(new Stroke(stylusPointCollection));
        }

        /// <inheritdoc />
        void IStylusSyncPlugin.StylusUp(RealTimeStylus sender, StylusUpData data)
        {
            
        }

        /// <inheritdoc />
        void IStylusSyncPlugin.StylusButtonDown(RealTimeStylus sender, StylusButtonDownData data)
        {
        }

        /// <inheritdoc />
        void IStylusSyncPlugin.StylusButtonUp(RealTimeStylus sender, StylusButtonUpData data)
        {
        }

        /// <inheritdoc />
        public void InAirPackets(RealTimeStylus sender, InAirPacketsData data)
        {
        }

        /// <inheritdoc />
        public void Packets(RealTimeStylus sender, PacketsData data)
        {
        }

        /// <inheritdoc />
        public void SystemGesture(RealTimeStylus sender, SystemGestureData data)
        {
        }

        /// <inheritdoc />
        public void TabletAdded(RealTimeStylus sender, TabletAddedData data)
        {
        }

        /// <inheritdoc />
        public void TabletRemoved(RealTimeStylus sender, TabletRemovedData data)
        {
        }

        /// <inheritdoc />
        public void CustomStylusDataAdded(RealTimeStylus sender, CustomStylusData data)
        {
        }

        /// <inheritdoc />
        public void Error(RealTimeStylus sender, ErrorData data)
        {
        }

        /// <inheritdoc />
        // Defines which handlers are called by the framework. We set the flags for pen-down, pen-up and pen-move.
        public DataInterestMask DataInterest =>DataInterestMask.StylusDown |
                                               DataInterestMask.Packets |
                                               DataInterestMask.StylusUp;
    }
}
