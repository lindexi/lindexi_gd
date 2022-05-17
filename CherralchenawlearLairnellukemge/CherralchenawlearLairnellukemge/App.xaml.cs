using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CherralchenawlearLairnellukemge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var rootResource = Resources;
            Dictionary<Uri, List<Uri>> dictionary = new Dictionary<Uri, List<Uri>>();
            FindMerged(rootResource, dictionary);

            base.OnStartup(e);
        }

        private void FindMerged(ResourceDictionary resourceDictionary, Dictionary<Uri, List<Uri>> dictionary)
        {
            foreach (var mergedDictionary in resourceDictionary.MergedDictionaries)
            {
                var source = mergedDictionary.Source;
                if (dictionary.TryGetValue(source,out var list))
                {
                    list.Add(resourceDictionary.Source);
                }
                else
                {
                    dictionary[source] = new List<Uri>()
                    {
                        resourceDictionary.Source
                    };
                }

                FindMerged(mergedDictionary, dictionary);
            }
        }
    }
}
