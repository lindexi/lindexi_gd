using System.Net;

var request = WebRequest.Create("http://test.lindexi.com/CeloluqelHemfechairyeni");
request.Method = "POST";
var requestStream = await request.GetRequestStreamAsync();

using(var fileStream = File.Open(@"C:\lindexi\File", FileMode.Open, FileAccess.Read))
{
    //await fileStream.CopyToAsync(requestStream); // OOM

    var buffer = new byte[10240];
    var length = 0;
    while((length = fileStream.Read(buffer, 0, buffer.Length)) > 0)
    {
        await requestStream.WriteAsync(buffer, 0, length);

        await requestStream.FlushAsync(); // Nothing
    }
}

var response = await request.GetResponseAsync();
