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
        public viewmemorandum(model.model _model)
        {
            this._model = _model;
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


        private cmemorandum _memorandum = new cmemorandum();
        private model.model _model;
    }
}
