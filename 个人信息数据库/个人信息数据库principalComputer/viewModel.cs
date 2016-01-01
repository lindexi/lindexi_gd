using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库principalComputer.model;

namespace 个人信息数据库principalComputer
{
    public class viewModel : notify_property
    {
        public viewModel()
        {
            _model.PropertyChanged += _model_PropertyChanged;
            reminder = "上位机";
        }

        public void ce()
        {
            _model.reminder = "林德熙\r\n3113006277";
            _model.reminder = string.Format("数据库ip{0}\r\n数据库名{1}\r\n连接" , DataSource , InitialCatalog);
            
            _model.lianjie();
        }

        /// <summary>
        /// 数据库ip
        /// </summary>
        public string DataSource
        {
            set
            {
                _model.DataSource = value;
                OnPropertyChanged();
            }
            get
            {
                return _model.DataSource;
            }
        }
        /// <summary>
        /// 数据库名
        /// </summary>
        public string InitialCatalog
        {
            set
            {
                _model.InitialCatalog = value;
            }
            get
            {
                return _model.InitialCatalog;
            }
        } 

        private model.model _model
        {
            set;
            get;
        } = new model.model();

        private void _model_PropertyChanged(object sender , System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName , nameof(_model.reminder)))
            {
                reminder = string.Empty;
                reminder = _model.reminder;
            }
            //throw new NotImplementedException();
        }
    }
}
