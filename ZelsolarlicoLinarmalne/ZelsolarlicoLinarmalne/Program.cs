using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZelsolarlicoLinarmalne
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var develhouguXikiwhallheserta = new DevelhouguXikiwhallheserta();
            develhouguXikiwhallheserta.Read("[x=1,y=2,w=3,h=5]");

            develhouguXikiwhallheserta = new DevelhouguXikiwhallheserta();
            develhouguXikiwhallheserta.Read("[");
        }
    }

    internal class DevelhouguXikiwhallheserta
    {
        public Rect Read(string str)
        {
            var manage = new Manage(new StringReader(str));
            manage.Read();

            if (manage.IsError)
            {
                throw new ArgumentException(manage.ErrString);
            }


            return new Rect();
        }

        private StringReader _read;
    }

    internal class SquareBracketsRead : IRead
    {
        /// <inheritdoc />
        public void Read(Manage manage)
        {
            if (manage.ReadChar() == '[')
            {
                manage.ReadProperty();
            }
            else
            {
                manage.Error("不是使用[开始");
            }
        }
    }

    internal class PropertyRead : IRead
    {
        /// <inheritdoc />
        public void Read(Manage manage)
        {
            var str = manage.ReadChar();
            string property;
            switch (str)
            {
                case 'w':
                case 'W':
                    property = "w";
                    break;
                case 'h':
                case 'H':
                    property = "h";
                    break;
                case 'x':
                case 'X':
                    property = "x";
                    break;
                case 'y':
                case 'Y':
                    property = "y";
                    break;
                default:
                    manage.Error("发现无法识别字符" + str);
                    return;
            }

            if (manage.ExitsProperty.Contains(property))
            {
                manage.Error("存在重复的属性" + property);
                return;
            }

            manage.SetCurrentProperty(property);

            manage.ReadEqual();
        }
    }

    internal class EqualRead : IRead
    {
        /// <inheritdoc />
        public void Read(Manage manage)
        {
            if (manage.ReadChar() == '=')
            {
                manage.ReadDouble();
            }
            else
            {
                manage.Error($"格式不对，在{manage.CurrentProperty}后面不是等于");
            }
        }
    }

    internal class DoubleRead : IRead
    {
        /// <inheritdoc />
        public void Read(Manage manage)
        {
            var str = new StringBuilder();
            while (true)
            {
                var c = manage.ReadChar();
                if (c <= '9' && c >= '0')
                {
                    str.Append(c);
                }
                else if (c == '.')
                {
                    str.Append(c);
                }
                else if (c == ',')
                {
                    if (ParseDouble(manage, str))
                    {
                        manage.ReadProperty();
                        return;
                    }
                }
                else if (c == ']')
                {
                    ParseDouble(manage, str);

                    return;
                }
            }
        }

        private bool ParseDouble(Manage manage, StringBuilder str)
        {
            if (double.TryParse(str.ToString(), out var n))
            {
                manage.SetCurrentProperty(n);
                return true;
            }

            manage.Error("无法将" + str + "转换");
            return false;
        }
    }

    internal class Manage
    {
        public Manage(StringReader read)
        {
            _read = read;
        }

        public string CurrentProperty { private set; get; }

        public HashSet<string> ExitsProperty { get; } = new HashSet<string>();

        public Rect Rect => _rect;

        public bool IsError { get; set; }

        public string ErrString { get; set; }

        public void SetCurrentProperty(string str)
        {
            ExitsProperty.Add(str);
            CurrentProperty = str;
        }

        public void ReadDouble()
        {
            _doubleRead.Read(this);
        }

        public void SetCurrentProperty(double value)
        {
            switch (CurrentProperty)
            {
                case "w":
                    _rect.Width = value;
                    break;
                case "h":
                    _rect.Height = value;
                    break;
                case "x":
                    _rect.X = value;
                    break;
                case "y":
                    _rect.Y = value;
                    break;
            }
        }

        public void ReadEqual()
        {
            _equalRead.Read(this);
        }

        public void ReadSquareBrackets()
        {
            _squareBracketsRead.Read(this);
        }

        public void ReadProperty()
        {
            _propertyRead.Read(this);
        }

        public void Error(string str)
        {
            IsError = true;
            ErrString = str;
        }

        public void Read()
        {
            ReadSquareBrackets();
        }

        public char ReadChar()
        {
            return (char) _read.Read();
        }

        private readonly IRead _doubleRead = new DoubleRead();
        private readonly IRead _equalRead = new EqualRead();
        private readonly IRead _propertyRead = new PropertyRead();
        private readonly IRead _squareBracketsRead = new SquareBracketsRead();

        private readonly StringReader _read;

        private Rect _rect;
    }

    internal interface IRead
    {
        void Read(Manage manage);
    }

    internal struct Rect
    {
        public double X { set; get; }

        public double Y { set; get; }

        public double Width { set; get; }
        public double Height { set; get; }
    }
}