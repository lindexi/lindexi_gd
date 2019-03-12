using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tallyBook
{
    public class viewModel:notify_property
    {
        public viewModel()
        {
            
        }
        DateTime dt = DateTime.Now;
        public void ce()
        {
            dt = dt.AddDays(-1);
            reminder = dt.ToString();

        }
    }
}
