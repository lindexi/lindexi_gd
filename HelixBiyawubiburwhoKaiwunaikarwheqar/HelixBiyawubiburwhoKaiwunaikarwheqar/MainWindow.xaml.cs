using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;

namespace HelixBiyawubiburwhoKaiwunaikarwheqar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var objReader = new ObjReader();
            //var model3DGroup = objReader.Read(@"NergelukarRemdawcerecayle\AnyConv.com__NergelukarRemdawcerecayle.obj");

            //Model.Content = model3DGroup;
            var stLReader = new StLReader();
            Model.Content = stLReader.Read(@"NergelukarRemdawcerecayle\AnyConv.com__NergelukarRemdawcerecayle.stl");

            if (Model.Content is Model3DGroup model3DGroup)
            {
                if (model3DGroup.Children[0] is GeometryModel3D geometryModel3D)
                {
                    var texture = new BitmapImage(new Uri(System.IO.Path.GetFullPath(@"NergelukarRemdawcerecayle\03bb4e56-7c3b-49f4-b7dd-f63fb6222510")));
                    geometryModel3D.Material = MaterialHelper.CreateMaterial(new ImageBrush(texture));
                }
            }
        }
    }
}
