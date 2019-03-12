using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Sockets;

namespace modelbusiness
{
    public class model
    {
        public model()
        {

        }

        public void ce()
        {
            
        }

        /// <summary>
        /// 端口
        /// </summary>
        public int post
        {
            set
            {
                _post = value;
            }
            get
            {
                return _post;
            }

        }
        /// <summary>
        /// 
        /// </summary>
        public string ip
        {
            set
            {
                _ip = value;
            }
            get
            {
                return _ip;
            }

        }
        public int id
        {
            set
            {
                _id = value;
            }
            get
            {
                return _id;
            }
        }

        
       
        public void send(string str, int id)
        {

        }

        private bool _server
        {
            set;
            get;
        }

        private int _id;

        private int _post;

        private string _ip;

       
    }    







    public class student:notify_property
    {
        public string name
        {
            set
            {
                _name = value;
                OnPropertyChanged("name");
            }
            get
            {
                return _name;
            }
        }
        public string city
        {
            set
            {
                _city = value;
                OnPropertyChanged("city");
            }
            get
            {
                return _city;
            }
        }
        public string age
        {
            set
            {
                _age = value;
                OnPropertyChanged("age");
            }
            get
            {
                return _age;
            }
        }
        public Windows.UI.Xaml.Media.ImageSource img
        {
            set
            {
                _img = value;
                OnPropertyChanged("img");
            }
            get
            {
                return _img;
            }
        }
        private string _name;
        private string _city;
        private string _age;
        private Windows.UI.Xaml.Media.ImageSource _img;

    }
}
