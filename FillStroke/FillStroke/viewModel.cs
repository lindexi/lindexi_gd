using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
namespace FillStroke
{
    public partial class viewModel: notify_property
    {
        public viewModel()
        {
            //reminder = ApplicationData.Current.LocalFolder.ToString();
            reminder = ApplicationData.Current.LocalFolder.Path;
        }
        

      
    }
}
