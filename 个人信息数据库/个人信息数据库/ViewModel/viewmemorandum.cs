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
            _model.memorandum = lmemorandum;
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
            if (memorandum.accord)
            {
                foreach (var temp in lmemorandum)
                {
                    if (memorandum_equals(temp))
                    {
                        warn = "输入重复";
                        return;
                    }
                }
                reminder = "添加日记";
                _model.send(ecommand.addmemorandum , memorandum.ToString());
                memorandum = new cmemorandum();
            }
            else
            {
                warn = "输入信息有误";
            }
        }
        public void delete()
        {
            if (memorandum.Equals(_item))
            {
                _model.send(ecommand.dmemorandum , _item.ToString());
                reminder = "删除备忘";
            }
            else
            {
                warn = "没有选择要删除备忘或备忘已修改";
                return;
            }
        }

        public void select()
        {
            for (int i = 0; i < lmemorandum.Count; i++)
            {
                if (!memorandum_equals(lmemorandum[i]))
                {
                    lmemorandum.RemoveAt(i);
                    i--;
                }
            }
            reminder = "查询备忘";
        }

        public void modify()
        {
            if (!memorandum.accord)
            {
                warn = "输入信息有误";
            }

            if (memorandum.Equals(_item))
            {
                warn = "没有修改";
            }
            else
            {
                if (_item == null)
                {
                    warn = "没有选择备忘";
                }
                else
                {
                    memorandum.Clone(_item);
                    _model.send(ecommand.newmemorandum , memorandum.ToString());
                    reminder = "修改备忘";
                }
            }
            reminder = "修改备忘";
        }

        public void eliminate()
        {
            _item = null;
            memorandum = new cmemorandum();
            reminder = "清除";
        }
        public void navigated()
        {
            //warn = "点击修改把现有表修改到数据库，按delete删除行,双击修改列";
            warn = "";
        }
        public void selectitem(System.Collections.IList item)
        {
            if (item.Count == 0)
            {
                return;
            }
            _item = item[0] as cmemorandum;
            if (_item != null)
            {
                memorandum = _item.Clone() as cmemorandum;
            }
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
        private System.Windows.Visibility _visibility = System.Windows.Visibility.Hidden;
        private viewModel _viewModel;
        private cmemorandum _memorandum = new cmemorandum();
        private cmemorandum _item;
        private model.model _model;
        private bool memorandum_equals(cmemorandum a)
        {
            return access(memorandum.MTIME , a.MTIME) &&
                   access(memorandum.PLACE , a.PLACE) &&
                   access(memorandum.incident , a.incident) &&
                   access(memorandum.CONTACTSID , a.CONTACTSID);
        }

        private bool access(string anull , string b)
        {
            return string.IsNullOrEmpty(anull) || string.Equals(anull , b);
        }
    }
}
