using System;
using JekkereaNegcewajyaML.Model.DataModels;
using Microsoft.ML;

namespace JekkereaNegcewajya
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
            input.SentimentText = "pika is a doubi"; // 这句话是我加的

            // Try model on sample data
            ModelOutput result = predEngine.Predict(input);

            Console.WriteLine(result.Prediction);
        }
    }
}
