using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库.model;

namespace 个人信息数据库.ViewModel
{
    public class viewdiary:notify_property
    {
        public viewdiary(viewModel _viewModel,model.model _model)
        {
            this._model = _model;
            this._viewModel = _viewModel;

            _viewModel.diary = this;
        }

        public cdiary diary
        {
            set;
            get;
        } = new cdiary();

        public System.Collections.ObjectModel.ObservableCollection<cdiary> ldiary
        {
            set;
            get;
        } = new System.Collections.ObjectModel.ObservableCollection<cdiary>();
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
        private System.Windows.Visibility _visibility = System.Windows.Visibility.Hidden;
        private viewModel _viewModel;

        private model.model _model
        {
            set;
            get;
        }
    }
}
