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
        private System.Windows.Visibility _visibility= System.Windows.Visibility.Hidden;

        private viewModel _viewModel;
        private cproperty _property = new cproperty();
        private model.model _model;

    }
}
