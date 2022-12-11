using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2
{
    public class MainViewModel
    {

        public ObservableCollection<Root> Elements { get; } = new();

        public MainViewModel()
        {

            Elements.Add(AddLevel1(new Root() { DisplayText = "Root 1" }));
            Elements.Add(new Root() { DisplayText = "Root 2" });
        }

        private Root AddLevel1(Root root)
        {
            for(int i = 0; i < 10; i++)
            {
                root.Children.Add(AddLevel2(new Level1() { DisplayText = $"Level1 - {i}" }));
            }
            return root;
        }

        private Level1 AddLevel2(Level1 level1)
        {
            for (int i = 0; i < 10; i++)
            {
                level1.Children.Add(new Level2() { DisplayText = $"Level1 - {i}" });
            }
            return level1;
        }
    }
}
