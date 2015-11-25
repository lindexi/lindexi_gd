using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 个人信息数据库principalComputer.model;

namespace 个人信息数据库principalComputer
{
    public class viewModel:notify_property
    {
        public viewModel()
        {
            _principal_computer = new principal_Computer(str =>
              {
                  string temp = str.Trim('\0' , ' ');
                  if (!string.IsNullOrEmpty(temp))
                  {
                      reminder = temp;
                  }
              });
            reminder = "上位机";
        }
        public void ce()
        {

        }

        private principal_Computer _principal_computer;
        public System.Collections.ObjectModel.ObservableCollection<caddressBook>
         addressBook
        {
            set;
            get;
        }
    }
}
