using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace classifyapp
{
    public class model
    {
        private model()
        {

        }
        public static model cmodel()
        {
            if (_model == null)
            {
                _model = new model();
            }
            return _model;
        }
        public async void windowsapp(string ProductId)
        {
            string uri = $"ms-windows-store://pdp/?ProductId={ProductId}";
            await Windows.System.Launcher.LaunchUriAsync(new Uri(uri));
        }
        private static model _model
        {
            set;
            get;
        } = new model();
    }
}
