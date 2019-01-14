using System;
using System.IO;
using System.Text;
using lindexi.MVVM.Framework.ViewModel;

namespace CanaynaTacerebayQeebas
{
    public class ViewModel : NavigateViewModel
    {
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Code
        {
            get => _code;
            set
            {
                if (value == _code) return;
                _code = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public override void OnNavigatedFrom(object sender, object obj)
        {
            Storage();
            base.OnNavigatedFrom(sender, obj);
        }

        /// <inheritdoc />
        public override void OnNavigatedTo(object sender, object obj)
        {
            Read();
            base.OnNavigatedTo(sender, obj);
        }

        private string _code;
        private string _name;
        private string _text;

        private void Read()
        {
            try
            {
                var fileInfo = new FileInfo(File);
                if (fileInfo.Exists)
                {
                    using (var stream = fileInfo.OpenText())
                    {
                        var name = stream.ReadLine();
                        var code = stream.ReadLine();

                        Name = name;
                        Code = code;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Storage()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Code))
            {
                return;
            }

            var str = Name + "\n" + Code;
            System.IO.File.WriteAllText(File, str, Encoding.UTF8);
        }

        private const string File = "file.txt";
    }
}