using System.Windows;
using lindexi.MVVM.Framework.ViewModel;

namespace WaveLineDemo
{
    public class ViewModel : ViewModelBase
    {
        public string WaveLength
        {
            get => _waveLength;
            set
            {
                if (value == _waveLength) return;
                _waveLength = value;
                OnPropertyChanged();
            }
        }

        public string WaveHeight
        {
            get => _waveHeight;
            set
            {
                if (value == _waveHeight) return;
                _waveHeight = value;
                OnPropertyChanged();
            }
        }

        public string CurveSquaring
        {
            get => _curveSquaring;
            set
            {
                if (value == _curveSquaring) return;
                _curveSquaring = value;
                OnPropertyChanged();
            }
        }

        public string BorderThickness
        {
            get => _borderThickness;
            set
            {
                if (value == _borderThickness) return;
                _borderThickness = value;
                OnPropertyChanged();
            }
        }

        public string FirstPointY
        {
            get => _firstPointY;
            set
            {
                if (value == _firstPointY) return;
                _firstPointY = value;
                OnPropertyChanged();
            }
        }

        public string SecondPointX
        {
            get => _secondPointX;
            set
            {
                if (value == _secondPointX) return;
                _secondPointX = value;
                OnPropertyChanged();
            }
        }

        public string SecondPointY
        {
            set
            {
                if (value == _secondPointY) return;
                _secondPointY = value;
                OnPropertyChanged();
            }
            get => _secondPointY;
        }

        public string FirstPointX
        {
            set
            {
                if (value == _firstPointX) return;
                _firstPointX = value;
                OnPropertyChanged();
            }
            get => _firstPointX;
        }

        public WaveLine Draw()
        {
            double.TryParse(FirstPointX, out var firstPointX);
            double.TryParse(FirstPointY, out var firstPointY);
            double.TryParse(SecondPointX, out var secondPointX);
            double.TryParse(SecondPointY, out var secondPointY);
            double.TryParse(BorderThickness, out var borderThickness);
            double.TryParse(CurveSquaring, out var curveSquaring);
            double.TryParse(WaveHeight, out var waveHeight);
            double.TryParse(WaveLength, out var waveLength);

            var waveLine = new WaveLine
            {
                BorderThickness = borderThickness,
                CurveSquaring = curveSquaring,
                WaveHeight = waveHeight,
                WaveLength = waveLength
            };
            waveLine.DrawWaveLine(new Point(firstPointX, firstPointY), new Point(secondPointX, secondPointY));
            return waveLine;
        }

        /// <inheritdoc />
        public override void OnNavigatedFrom(object sender, object obj)
        {
        }

        /// <inheritdoc />
        public override void OnNavigatedTo(object sender, object obj)
        {
        }

        private string _borderThickness = 1.ToString();
        private string _curveSquaring = 0.57.ToString();
        private string _firstPointX = 50.ToString();
        private string _firstPointY = 290.ToString();
        private string _secondPointX = 760.ToString();
        private string _secondPointY = 226.ToString();
        private string _waveHeight = 90.ToString();
        private string _waveLength = 100.ToString();
    }
}