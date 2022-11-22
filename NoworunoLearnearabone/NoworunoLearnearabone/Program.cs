
// Add using statements
using Microsoft.ML;
using Microsoft.ML.Tokenizers;

// Initialize MLContext
//var mlContext = new MLContext();

// Define vocabulary file paths
var vocabFilePath = @"F:\lindexi\Code\Tokenizer\vocab.json";
var mergeFilePath = @"F:\lindexi\Code\Tokenizer\merges.txt";

// Initialize Tokenizer
var tokenizer = new Tokenizer(new Bpe(vocabFilePath, mergeFilePath), RobertaPreTokenizer.Instance);

// Define input for tokenization
var input = "the brown fox jumped over the lazy dog!";

// Encode input
var tokenizerEncodedResult = tokenizer.Encode(input);

// Decode results
var result = tokenizer.Decode(tokenizerEncodedResult.Ids);

Console.WriteLine();