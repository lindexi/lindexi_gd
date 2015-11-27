using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库.model;
namespace 个人信息数据库.ViewModel
{
    public class viewaddressBook:notify_property
    {
        public viewaddressBook(viewModel _viewModel,model.model _model)
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
            reminder = "添加通讯";
        }
        public void delete()
        {
            reminder = "删除通讯";
        }

        public void select()
        {
            reminder = "查询通讯";
        }

        public void modify()
        {
            reminder = "修改通讯";
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
        private model.model _model
        {
            set;
            get;
        }

        private caddressBook _addressBook = new caddressBook();
    }
}
