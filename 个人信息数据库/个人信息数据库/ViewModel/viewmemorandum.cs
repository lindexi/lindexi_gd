using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库.model;

namespace 个人信息数据库.ViewModel
{
    public class viewmemorandum : notify_property
    {
        public viewmemorandum(viewModel _viewModel,model.model _model)
        {
            this._model = _model;
            this._viewModel = _viewModel;

            _viewModel.memorandum = this;
        }

        public cmemorandum memorandum
        {
            set
            {
                _memorandum = value;
                OnPropertyChanged();
            }
            get
            {
                return _memorandum;
            }
        }

        public System.Collections.ObjectModel.ObservableCollection<cmemorandum> lmemorandum
        {
            set;
            get;
        } = new System.Collections.ObjectModel.ObservableCollection<cmemorandum>();
        public System.Windows.Visibility visibility
        {
            set
            {
                _visibility = value;
                OnPropertyChanged();
            }
            get
            {
                return _visibility;
            }
        }

        public new string reminder
        {
            set
            {
                _model.reminder = value;
            }
            get
            {
                return _model.reminder;
            }
        }

        public void add()
        {
            reminder = "添加备忘";
        }
        public void delete()
        {
            reminder = "删除备忘";
        }

        public void select()
        {
            reminder = "查询备忘";
        }

        public void modify()
        {
            reminder = "修改备忘";
        }

        public void eliminate()
        {
            reminder = "清除";
        }
        public string warn
        {
            set
            {
                _warn = value;
                OnPropertyChanged();
            }
            get
            {
                return _warn;
            }
        }
        private string _warn = "输入";
        private System.Windows.Visibility _visibility = System.Windows.Visibility.Hidden;
        private viewModel _viewModel;
        private cmemorandum _memorandum = new cmemorandum();
        private model.model _model;
    }
}
