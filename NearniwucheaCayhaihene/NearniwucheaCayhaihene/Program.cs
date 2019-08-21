using System;
using Microsoft.ML;
using NearniwucheaCayhaiheneML.Model.DataModels;

namespace NearniwucheaCayhaihene
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load the model
            MLContext mlContext = new MLContext();
            ITransformer mlModel = mlContext.Model.Load("MLModel.zip", out var modelInputSchema);
            var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);

            // Use the code below to add input data
            var input = new ModelInput();
            // input.
            input.SentimentText = "==RUDE== Dude, you are rude upload that carl picture back, or else.";

            // Try model on sample data
            ModelOutput result = predEngine.Predict(input);

            Console.WriteLine(result.Prediction);


            input = new ModelInput()
            {
                SentimentText = "::::::::::I'm not sure either. I think it has something to do with merely ahistorical vs being derived from pagan myths. Price does believe the latter, I'm not sure about other CMT proponents. "
            };
            result = predEngine.Predict(input);
            Console.WriteLine(result.Prediction);
        }
    }
}
