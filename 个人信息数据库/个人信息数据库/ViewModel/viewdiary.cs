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
        public viewdiary(model.model _model)
        {
            this._model = _model;
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

        private model.model _model
        {
            set;
            get;
        }
    }
}
