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
        public viewaddressBook(model.model _model)
        {
            this._model = _model;
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

        private model.model _model
        {
            set;
            get;
        }

        private caddressBook _addressBook = new caddressBook();
    }
}
