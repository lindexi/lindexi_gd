using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CibairyafocairluYerkinemde
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 10; i++)
            {
                Dogs.Add(new Dog()
                {
                    Name = "Dog" + i
                });

                Cats.Add(new Cat()
                {
                    Name = "Cat" + i
                });
            }

            DataContext = this;
        }

        public ObservableCollection<Dog> Dogs { get; } = new ObservableCollection<Dog>();
        public ObservableCollection<Cat> Cats { get; } = new ObservableCollection<Cat>();
    }

    public class Dog : Animal
    {
    }

    public class Cat : Animal
    {
    }

    public class Animal
    {
        public string Name { get; set; }
    }

    public class CompositeCollectionConverter : IMultiValueConverter
    {
        public static readonly CompositeCollectionConverter Default = new CompositeCollectionConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var compositeCollection = new CompositeCollection();
            foreach (var value in values)
            {
                if (value is IEnumerable enumerable)
                {
                    compositeCollection.Add(new CollectionContainer { Collection = enumerable });
                }
                else
                {
                    compositeCollection.Add(value);
                }
            }

            return compositeCollection;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("CompositeCollectionConverter ony supports oneway bindings");
        }
    }
}
