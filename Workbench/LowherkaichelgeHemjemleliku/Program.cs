// See https://aka.ms/new-console-template for more information

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

NetworkStream ns = null!;

var sslStream = new SslStream(ns);
await sslStream.AuthenticateAsClientAsync("asd.asd");

sslStream.AuthenticateAsServerAsync(new X509Certificate())

Console.WriteLine("Hello, World!");
