using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModel;
namespace incorporates
{
    public class viewModel:notify_property
    {
        public viewModel()
        {
            _property = new property();

        }
        public property p
        {
            set
            {
                _property = value;
            }
            get
            {
                return _property;
            }
        }
        private property _property;

        
    }
}
