using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace JardalllojoHayeajemjuli
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            NeuronTask = Task.Run(() =>
            {
                var data = new[]
                {
                    new Data()
                    {
                        Input = new double[] {0, 0},
                        Output = 0
                    },
                    new Data()
                    {
                        Input = new double[] {0, 1},
                        Output = 1
                    },
                    new Data()
                    {
                        Input = new double[] {1, 0},
                        Output = 1
                    },
                    new Data()
                    {
                        Input = new double[] {1, 1},
                        Output = 1
                    },
                };

                var learning = new Learning();
                return learning.Learn(data);
            });
        }

        private async void Button_OnClicked(object sender, EventArgs e)
        {
            var neuron = await NeuronTask;

            var n1Text = N1Text.Text;
            var n2Text = N2Text.Text;

            if (int.TryParse(n1Text, out var n1))
            {
                if (int.TryParse(n2Text, out var n2))
                {
                    Text.Text = neuron.Compute(new double[] {n1, n2}).ToString();
                }
            }
        }

        private Task<Neuron> NeuronTask { get; }
    }
}
