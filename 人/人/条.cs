using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace 人
{
    public class ct_条:ViewModel.notify_property
    {
        public ct_条()
        {
            name_名字 = "";
            max_最大数 = 100;
            num = 100;
            恢复 = 0;
        }
        public ct_条(string name,double num,double max,double recover)
        {
            name_名字 = name;
            max_最大数 = max;
            this.num = num;
            恢复 = recover;
        }
        ~ct_条()
        {
        
        }

        public double 恢复
        {
            set
            {
                _recover = value;
                OnPropertyChanged("恢复");
            }
            get
            {
                return _recover;
            }
        }
        public double max_最大数
        {
            set
            {
                _max_num = value;
                OnPropertyChanged("percent");
                OnPropertyChanged("max_最大数");
            }
            get
            {
                return _max_num;
            }
        }
        public double num
        {
            set
            {
                _num = value;
                OnPropertyChanged("percent");
                OnPropertyChanged("num");
            }
            get
            {
                return _num;
            }
        }
        public string name_名字
        {
            set
            {
                _name = value;
            }
            get
            {
                return _name;
            }
        }
        public double percent
        {
            set
            {

            }
            get
            {
                return num / max_最大数;
            }
        }

        public override string ToString()
        {
            return name_名字 + " " + num.ToString() + "/" + max_最大数.ToString()+" ";
        }

        string _name;
        double _num;
        double _max_num;
        double _recover;
    }
}
