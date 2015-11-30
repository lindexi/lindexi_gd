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
            _model.diary = ldiary;
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
            if (diary.accord)
            {
                foreach (var temp in ldiary)
                {
                    if (diary_equals(temp))
                    {
                        warn = "输入重复";
                        return;
                    }
                }

                reminder = "添加日记";
                _model.send(ecommand.adddiary , diary.ToString());
                diary = new cdiary();
            }
            else
            {
                warn = "输入信息有误";
            }
        }
        public void delete()
        {
            reminder = "删除日记";
        }

        public void select()
        {
            reminder = "查询日记";
        }

        public void modify()
        {
            reminder = "修改日记";
        }

        public void eliminate()
        {
            reminder = "清除";
        }
        public void navigated()
        {
            warn = "点击修改把现有表修改到数据库，按delete删除行,双击修改列";
        }
        public void selectitem(System.Collections.IList item)
        {

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
        private string _warn="输入";
        private System.Windows.Visibility _visibility = System.Windows.Visibility.Hidden;
        private viewModel _viewModel;

        private model.model _model
        {
            set;
            get;
        }

        private bool diary_equals(cdiary a)
        {
            return access(diary.MTIME , a.MTIME) &&
                   access(diary.PLACE , a.PLACE) &&
                   access(diary.incident , a.incident) &&
                   access(diary.CONTACTSID , a.CONTACTSID);
        }
        private bool access(string anull , string b)
        {
            return string.IsNullOrEmpty(anull) || string.Equals(anull , b);
        }
    }
}
