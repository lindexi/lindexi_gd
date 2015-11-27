using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库.model;

namespace 个人信息数据库.ViewModel
{
    public class viewproperty : notify_property
    {
        public viewproperty(viewModel _viewModel,model.model _model)
        {
            this._model = _model;
            this._viewModel = _viewModel;

            _viewModel.property = this;
            _model.property = lproperty;

        }

        public cproperty Property
        {
            set
            {
                _property = value;
                OnPropertyChanged();
            }
            get
            {
                return _property;
            }
        }
        public System.Collections.ObjectModel.ObservableCollection<cproperty> lproperty
        {
            set;
            get;
        } = new System.Collections.ObjectModel.ObservableCollection<cproperty>();
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
            reminder = "添加财物";
        }
        public void delete()
        {
            reminder = "删除财物";
        }

        public void select()
        {
            reminder = "查询财物";
        }

        public void modify()
        {
            reminder = "修改财物";
        }

        public void eliminate()
        {
            reminder = "清除";
        }

        public void navigated()
        {
            warn = "点击修改把现有表修改到数据库，按delete删除行,双击修改列";
        }

        public string warn
        {
            set
            {
                _warn = value;
                warnvisibility = System.Windows.Visibility.Visible;
                OnPropertyChanged();
            }
            get
            {
                return _warn;
            }
        }
        public System.Windows.Visibility warnvisibility
        {
            set
            {
                _warnvisibility = value;
                OnPropertyChanged();
            }
            get
            {
                return _warnvisibility;
            }
        }
        private System.Windows.Visibility _warnvisibility = System.Windows.Visibility.Hidden;
        private string _warn = "输入";
        private System.Windows.Visibility _visibility= System.Windows.Visibility.Hidden;

        private viewModel _viewModel;
        private cproperty _property = new cproperty();
        private model.model _model;

    }
}
