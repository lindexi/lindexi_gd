using System;
using System.IO;
using System.Net.Http;

namespace LeewhajabeburKenejallkelyo
{
    class Program
    {
        static void Main(string[] args)
        {
            var httpClient = new HttpClient();
            var url = "http://localhost:5732";
            url += "/testpptx2courseware/ConvertWordFile";

            var file = @"Test-04.docx";

            var fileStream = File.OpenRead(file);

            var multipartFormDataContent = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            multipartFormDataContent.Add(streamContent,"File", "TaskPlanTemplate.docx");

            var httpResponseMessage = httpClient.PostAsync(url, multipartFormDataContent).Result;
            var result = httpResponseMessage.Content.ReadAsStringAsync().Result;

            Console.WriteLine(url+result);
        }
    }
}
