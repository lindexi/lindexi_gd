// See https://aka.ms/new-console-template for more information

var socketsHttpHandler = new SocketsHttpHandler();
var httpMessageInvoker = new HttpMessageInvoker(socketsHttpHandler, disposeHandler: true);

Console.WriteLine("Hello, World!");
