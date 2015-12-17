using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace produproperty
{
    class viewModel : notify_property
    {
        public viewModel()
        {
            _m = new model();
        }
        public bool firstget
        {
            set;
            get;
        }
        public bool updateproper
        {
            set;
            get;
        }
        public string text
        {
            set
            {
                _text = value;
                OnPropertyChanged();               
            }
            get
            {
                return _text;
            }
        }


        public void property()
        {
            text = _m.property(text , firstget , updateproper);
        }
        private string _text;
        private model _m;
    }
}
