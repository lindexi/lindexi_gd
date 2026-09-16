namespace WinRemoteShell.Client;

internal static class HttpResponseMessageExtensions
{
    internal static async Task EnsureSuccessWithDetailsAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var serverError = (await response.Content.ReadAsStringAsync(cancellationToken)).TrimEnd();
        var message = string.IsNullOrWhiteSpace(serverError)
            ? $"The remote server returned {(int) response.StatusCode} ({response.ReasonPhrase}) without error details. The server may be an older WinRemoteShell version."
            : $"The remote server returned {(int) response.StatusCode} ({response.ReasonPhrase}):{Environment.NewLine}{serverError}";
        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
