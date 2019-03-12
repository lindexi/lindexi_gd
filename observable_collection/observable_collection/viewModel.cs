using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace observable_collection
{
    public partial class viewModel : notify_property
    {
        public viewModel()
        {

        }

        public System.Collections.ObjectModel.ObservableCollection<caddressBook> addressBook
        {
            set;
            get;
        }
            = new System.Collections.ObjectModel.ObservableCollection<caddressBook>()
        {
                new caddressBook()
                {
                    name ="张三",
                    contact ="0",
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

        /// <summary>
        /// 修改
        /// </summary>
        public void modify()
        {
            var taddressBook = new caddressBook()
            {
                name = "张三" ,
                contact = "2" ,
                address = "中国" ,
                city = " " ,
                comment = " "
            };
            addressBook.Add(taddressBook);
        }

    }
}
