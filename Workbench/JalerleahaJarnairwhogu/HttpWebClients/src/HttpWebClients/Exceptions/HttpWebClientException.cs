namespace HttpWebClients.Exceptions;

public class HttpWebClientException : System.Exception
{
    public HttpWebClientException()
    {
    }

    public HttpWebClientException(string? message) : base(message)
    {
    }

    public HttpWebClientException(string? message, System.Exception? innerException) : base(message, innerException)
    {
    }
}