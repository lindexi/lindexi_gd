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
        public viewproperty(viewModel _viewModel , model.model _model)
        {
            this._model = _model;
            this._viewModel = _viewModel;

            _viewModel.property = this;
            _model.property = lproperty;

            lproperty.CollectionChanged += Lproperty_CollectionChanged;
        }

     

        public cproperty property
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
            set
            {
                _lproperty = value;
                OnPropertyChanged();
            }
            get
            {                
                return _lproperty;
            }
        } 
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
        public string money
        {
            set
            {
                value = string.Empty;
                OnPropertyChanged();
            }
            get
            {
                decimal sum;
                sum = 0;
                decimal i;
                i = 0;
                foreach (var temp in lproperty)
                {
                    try
                    {
                        i = Convert.ToDecimal(temp.PMONEY);
                    }
                    catch
                    {

                    }
                    sum += i;
                }
                return sum.ToString();
            }
        }
        public void add()
        {
            if (property.accord)
            {
                foreach (var temp in lproperty)
                {
                    if (property_equals(temp))
                    {
                        warn = "输入重复";
                        return;
                    }
                }
                reminder = "添加财物";
                _model.send(ecommand.addproperty , property.ToString());
                property = new cproperty();
            }
            else
            {
                warn = "输入信息有误";
            }
        }
        public void delete()
        {
            if (property.Equals(_item))
            {
                _model.send(ecommand.dproperty , _item.ToString());
                reminder = "删除财物";
            }
            else
            {
                warn = "没有选择要删除日记或日记已修改";
                return;
            }
        }

        public void select()
        {
            for (int i = 0; i < lproperty.Count; i++)
            {
                if (!property_equals(lproperty[i]))
                {
                    lproperty.RemoveAt(i);
                    i--;
                }
            }
            reminder = "查询财物";
        }

        public void modify()
        {
            if (!property.accord)
            {
                warn = "输入信息有误";
            }

            if (property.Equals(_item))
            {
                warn = "没有修改";
            }
            else
            {
                if (_item == null)
                {
                    warn = "没有选择财物";
                }
                else
                {
                    property.Clone(_item);
                    _model.send(ecommand.newproperty , property.ToString());
                    reminder = "修改财物";
                }
            }
        }

        public void eliminate()
        {
            _item = null;
            property = new cproperty();
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
            _item = item[0] as cproperty;
            if (_item != null)
            {
                property = _item.Clone() as cproperty;
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
        private cproperty _property = new cproperty();
        private cproperty _item = new cproperty();
        private model.model _model;
        private System.Collections.ObjectModel.ObservableCollection<cproperty> _lproperty = new System.Collections.ObjectModel.ObservableCollection<cproperty>();
        private bool property_equals(cproperty a)
        {
            return access(property.MTIME , a.MTIME) &&
                   access(property.terminal , a.terminal) &&
                   access(property.PMONEY , a.PMONEY) &&
                   access(property.MTIME , a.MTIME) &&
                   access(property.CONTACTSID , a.CONTACTSID);
        }
        private void Lproperty_CollectionChanged(object sender , System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("money");
        }
        private bool access(string anull , string b)
        {
            return string.IsNullOrEmpty(anull) || string.Equals(anull , b);
        }
    }
}
