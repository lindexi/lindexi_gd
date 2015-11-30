using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库.model;
namespace 个人信息数据库.ViewModel
{
    public class viewaddressBook : notify_property
    {
        public viewaddressBook(viewModel _viewModel , model.model _model)
        {
            this._model = _model;
            this._viewModel = _viewModel;

            _viewModel.addressbook = this;
            _model.addressbook = laddressBook;

        }

        public caddressBook addressBook
        {
            set
            {
                _addressBook = value;
                OnPropertyChanged();
            }
            get
            {
                return _addressBook;
            }
        }
        public System.Collections.ObjectModel.ObservableCollection<caddressBook>
        /*public List<caddressBook>*/ laddressBook
        {
            set;
            get;
        } =
           //new List<caddressBook>()
           new System.Collections.ObjectModel.ObservableCollection<caddressBook>()
           {
                //new caddressBook()
                //{
                //    name ="通讯人姓名",
                //    contact ="联系方式",
                //    address ="工作地点",
                //    city ="城市",
                //    comment ="备注"
                //},
                new caddressBook()
                {
                    name ="张三",
                    contact ="1",
                    address ="中国",
                    city =" ",
                    comment =" "
                } ,
                new caddressBook()
                {
                    name ="张三",
                    contact ="1",
                    address ="中国",
                    city =" ",
                    comment =" "
                }
            };
        //public void warn()
        //{

        //}
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
            if (accord())
            {
                foreach (var t in laddressBook)
                {
                    if (addressbook_equals(addressBook, t))
                    {
                        warn = "信息重复";
                        return;
                    }
                }
                _model.send(ecommand.addaddressBook , addressBook.ToString());
                reminder = "添加通讯";
                addressBook = new caddressBook();
            }
        }
        public void delete()
        {          
            if (addressBook.Equals(_item))
            {
                _model.send(ecommand.daddressBook , _item.ToString());
                reminder = "删除通讯";
            }
            else
            {
                warn = "没有选择要删除通讯录或通讯录已修改";
                return;
            }
        }

        public void select()
        {
            int i = 0;

            for (i = 0; i < laddressBook.Count; i++)
            {
                if (!addressbook_equals(addressBook,laddressBook[i]))
                {
                    laddressBook.RemoveAt(i);
                    i--;
                }               
            }

            reminder = "查询通讯";
        }

       

        public void modify()
        {
            if (!accord())
            {
                return;
            }


            if (addressBook.Equals(_item))
            {
                reminder = "没有修改";
            }
            else
            {
                if (_item == null)
                {
                    warn = "没有选择通讯录";
                    return;
                }
                addressBook.Clone(_item);
                ctransmitter transmitter = new ctransmitter(_model.id , ecommand.newaddressBook , addressBook.ToString());

                _model.send(transmitter.ToString());

                reminder = "修改通讯";
            }

        }

        public void eliminate()
        {
            _item = null;
            addressBook = new caddressBook();
            reminder = "清除";
        }
        public void navigated()
        {
            warn = "点击修改把现有表修改到数据库，按delete删除行,双击修改列";
        }
        public void selectitem(System.Collections.IList item)
        {
            if (item.Count == 0)
            {
                return;
            }
            _item = item[0] as caddressBook;
            if (_item != null)
            {
                addressBook = _item.Clone() as caddressBook;
            }
        }
        public bool accord()
        {
            if (!addressBook.accord)
            {
                warn = "输入信息有误";
                return false;
            }
            else
            {
                warn = string.Empty;
                return true;
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
        private model.model _model
        {
            set;
            get;
        }
        private caddressBook _item;
        private caddressBook _addressBook = new caddressBook();
        private bool addressbook_equals(caddressBook anull , caddressBook b)
        {
            return  access(anull.name , b.name) &&
                    access(anull.contact , b.contact) &&
                    access(anull.address , b.address) &&
                    access(anull.city , b.city) &&
                    access(anull.comment , b.comment);
        }
        private bool access(string anull , string b)
        {
            return string.IsNullOrEmpty(anull) || string.Equals(anull , b);
        }
    }
}
