using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Serialization;

namespace KelerbelaChukoqayhi
{
    /// <summary>
    /// InkRecordUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class InkRecordUserControl : UserControl
    {
        public InkRecordUserControl()
        {
            InitializeComponent();

            //var random = new Random();

            //for (int i = 0; i < 100; i++)
            //{
            //    InkDataModelCollection.Add(new InkDataModel()
            //    {
            //        X = random.Next(100000) / 100.0,
            //        Y = random.Next(100000) / 100.0,
            //        PressureFactor = random.Next(100000) / 100.0,
            //    });
            //}
        }


        public ObservableCollection<InkDataModel> InkDataModelCollection { get; } =
            new ObservableCollection<InkDataModel>();
    }

    public class StrokeDataModelList : List<StrokeDataModel>
    {
        public string Serialize()
        {
            return XmlSerialize.Serialize(this);
        }

        public static StrokeDataModelList Deserialize(string text) =>
            XmlSerialize.Deserialize<StrokeDataModelList>(text);
    }

    public static class XmlSerialize
    {
        public static string Serialize<T>(T obj)
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            var xmlSerializer = new XmlSerializer(typeof(T));
            var stringBuilder = new StringBuilder();

            using (var xmlWriter = XmlWriter.Create(new StringWriter(stringBuilder), new XmlWriterSettings
                {
                    Indent = true
                }))
            {
                xmlSerializer.Serialize(xmlWriter, obj, ns);
            }

            return stringBuilder.ToString();
        }

        public static T Deserialize<T>(string text)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            return (T) xmlSerializer.Deserialize(new StringReader(text));
        }
    }

    public class StrokeDataModel : List<InkDataModel>
    {
        public StrokeDataModel()
        {
        }

        public StrokeDataModel(IEnumerable<InkDataModel> collection) : base(collection)
        {
        }
    }

    public class InkDataModel
    {
        public InkDataModel(StylusPoint stylusPoint, TimeSpan time)
        {
            X = stylusPoint.X;
            Y = stylusPoint.Y;
            PressureFactor = stylusPoint.PressureFactor * 1024;

            Time = time;
        }

        public InkDataModel()
        {
        }

        public TimeSpan Time { set; get; }

        public double X { set; get; }

        public double Y { set; get; }

        public double PressureFactor { set; get; }
    }

    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                return d.ToString("0.00");
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class InkCollectionManager
    {
        public InkCollectionManager(InkRecordUserControl inkRecordUserControl, FrameworkElement inputElement)
        {
            InkRecordUserControl = inkRecordUserControl;
            InputElement = inputElement;

            InputElement.StylusDown += MainWindow_StylusDown;
            InputElement.StylusMove += MainWindow_StylusMove;
            InputElement.StylusUp += MainWindow_StylusUp;
        }

        private void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            StrokeDataModelList.Add(new StrokeDataModel(InkRecordUserControl.InkDataModelCollection));
        }

        public StrokeDataModelList StrokeDataModelList { get; } = new StrokeDataModelList();

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            var time = DateTime.Now - _lastTime;

            var stylusPointCollection = e.GetStylusPoints(InputElement);
            foreach (var stylusPoint in stylusPointCollection)
            {
                InkRecordUserControl.InkDataModelCollection.Add(new InkDataModel(stylusPoint, time));
            }
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            InkRecordUserControl.InkDataModelCollection.Clear();
            _lastTime = DateTime.Now;
        }

        private DateTime _lastTime;


        public InkRecordUserControl InkRecordUserControl { get; }

        public FrameworkElement InputElement { get; }
    }
}