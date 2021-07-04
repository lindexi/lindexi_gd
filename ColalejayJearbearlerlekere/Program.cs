using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace ColalejayJearbearlerlekere
{
    [MemoryDiagnoser]
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public object LookupContentTypeForImageUri(Uri imageUri)
        {
            //Extract file extension without '.'
            String path = imageUri.OriginalString;
            ReadOnlySpan<char> extension = Path.GetExtension(path).ToLower(CultureInfo.InvariantCulture).AsSpan(1);
            ContentType contentType;
            if (extension.Equals(XpsS0Markup.JpgExtension, StringComparison.Ordinal))
            {
                contentType = XpsS0Markup.JpgContentType;
            }
            else if (extension.Equals(XpsS0Markup.PngExtension, StringComparison.Ordinal))
            {
                contentType = XpsS0Markup.PngContentType;
            }
            else if (extension.Equals(XpsS0Markup.TifExtension, StringComparison.Ordinal))
            {
                contentType = XpsS0Markup.TifContentType;
            }
            else if (extension.Equals(XpsS0Markup.WdpExtension, StringComparison.Ordinal))
            {
                contentType = XpsS0Markup.WdpContentType;
            }
            else
            {
                //default to PNG
                contentType = XpsS0Markup.PngContentType;
            }
            return contentType;
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public object LookupContentTypeForImageUriIgnoreCase(Uri imageUri)
        {
            //Extract file extension without '.'
            ReadOnlySpan<char> path = imageUri.OriginalString.AsSpan();
            ReadOnlySpan<char> extension = Path.GetExtension(path).Slice(1);
            ContentType contentType;
            if (extension.Equals(XpsS0Markup.JpgExtension, StringComparison.OrdinalIgnoreCase))
            {
                contentType = XpsS0Markup.JpgContentType;
            }
            else if (extension.Equals(XpsS0Markup.PngExtension, StringComparison.OrdinalIgnoreCase))
            {
                contentType = XpsS0Markup.PngContentType;
            }
            else if (extension.Equals(XpsS0Markup.TifExtension, StringComparison.OrdinalIgnoreCase))
            {
                contentType = XpsS0Markup.TifContentType;
            }
            else if (extension.Equals(XpsS0Markup.WdpExtension, StringComparison.OrdinalIgnoreCase))
            {
                contentType = XpsS0Markup.WdpContentType;
            }
            else
            {
                //default to PNG
                contentType = XpsS0Markup.PngContentType;
            }
            return contentType;
        }

        public IEnumerable<Uri> GetArguments()
        {
            yield return  new Uri(@"C:\foo.jpg") ;
            yield return  new Uri(@"C:\foo.JPG") ;
            yield return  new Uri(@"C:\foo.png") ;
            yield return  new Uri(@"C:\foo.PNG") ;
            yield return  new Uri(@"C:\foo.tif") ;
            yield return  new Uri(@"C:\foo.TIF") ;
            yield return  new Uri(@"C:\foo.wdp") ;
            yield return  new Uri(@"C:\foo.WDP") ;
            yield return  new Uri(@"C:\foo.none");
            yield return  new Uri(@"C:\foo.NONE");
        }
    }
}
