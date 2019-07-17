using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EncodingUtf8AndGBKDifferentiater;

namespace Babukeelleneeoai
{
    public class ConvertFileEncodingModel : INotifyPropertyChanged
    {
        private FileInfo _file;
        private Encoding _encoding;
        private Encoding _convertEncoding;
        private string _trace;

        /// <inheritdoc />
        public ConvertFileEncodingModel()
        {
            var optionEncoding = new List<Encoding>()
            {
                System.Text.Encoding.UTF8,
                System.Text.Encoding.GetEncoding("GBK")
            };

            foreach (var temp in System.Text.Encoding.GetEncodings().Select(temp => temp.GetEncoding()))
            {
                if (optionEncoding.All(encoding => encoding.EncodingName != temp.EncodingName))
                {
                    optionEncoding.Add(temp);
                }
            }

            OptionEncoding = optionEncoding;
        }

        public List<Encoding> OptionEncoding { get; }

        public Encoding Encoding
        {
            set
            {
                if (value == _encoding) return;
                _encoding = value;
                OnPropertyChanged();
            }
            get => _encoding;
        }

        public Encoding ConvertEncoding
        {
            get
            {
                if (_convertEncoding is null)
                {
                    _convertEncoding = OptionEncoding[0];
                }

                return _convertEncoding;
            }
            set
            {
                if (value == _convertEncoding) return;
                _convertEncoding = value;
                OnPropertyChanged();
            }
        }

        public FileInfo File
        {
            get => _file;
            set
            {
                _file = value;

                DifferentiateEncoding();
            }
        }

        public string Trace
        {
            set
            {
                _trace = value;
                OnPropertyChanged();
            }
            get => _trace;
        }

        private void DifferentiateEncoding()
        {
            var (encoding, confidenceCount) = EncodingDifferentiater.DifferentiateEncoding(File);

            if (encoding.BodyName == System.Text.Encoding.UTF8.BodyName)
            {
                Encoding = OptionEncoding[0];
            }
            else if (encoding.BodyName == System.Text.Encoding.GetEncoding("GBK").BodyName)
            {
                Encoding = OptionEncoding[1];
            }
            else if (encoding.BodyName == System.Text.Encoding.ASCII.BodyName)
            {
                Encoding = Encoding.ASCII;
            }
            else
            {
                Encoding = encoding;
            }
        }

        private Encoding GetEncoding(string encoding)
        {
            switch (encoding)
            {
                case "Utf8": return System.Text.Encoding.UTF8;
                case "GBK": return System.Text.Encoding.GetEncoding("GBK");
                default: return System.Text.Encoding.GetEncoding(encoding);
            }
        }

        public bool ConvertFile()
        {
            try
            {
                string str;
                using (var stream = new StreamReader(File.OpenRead(), Encoding))
                {
                    str = stream.ReadToEnd();
                }

                using (var stream = new StreamWriter(File.Open(FileMode.Create), ConvertEncoding))
                {
                    stream.Write(str);
                }

                Trace = "Success";
            }
            catch (Exception e)
            {
                Trace = e.ToString();
                return false;
            }

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}