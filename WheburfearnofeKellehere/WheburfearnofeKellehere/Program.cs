using System;
using System.Collections.Generic;
using System.Text;

namespace WheburfearnofeKellehere
{
    public class Program
    {
        [System.STAThreadAttribute]
        public static void Main()
        {
            using (new WheburfearnofeKellehere.XamlIslandApp.App())
            {
                var app = new WheburfearnofeKellehere.App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
