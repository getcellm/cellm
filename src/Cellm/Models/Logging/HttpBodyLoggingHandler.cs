using Microsoft.Extensions.Logging;

namespace Cellm.Models.Logging;

internal class HttpBodyLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpBodyLoggingHandler> _logger;
    private readonly int _maxBodyLength;

    public HttpBodyLoggingHandler(ILogger<HttpBodyLoggingHandler> logger, int maxBodyLength = 32768)
    {
        _logger = logger;
        _maxBodyLength = maxBodyLength;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Log request
        _logger.LogInformation("HTTP {Method} {Url}", request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            if (requestBody.Length > _maxBodyLength)
            {
                requestBody = requestBody.Substring(0, _maxBodyLength) + "... (truncated)";
            }
            _logger.LogInformation("Request Body: {Body}", requestBody);
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Log response
        _logger.LogInformation("Response Status: {StatusCode}", (int)response.StatusCode);

        // Read response body (and buffer it so the caller can read it again)
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (responseBody.Length > _maxBodyLength)
        {
            _logger.LogInformation("Response Body: {Body}... (truncated)", responseBody.Substring(0, _maxBodyLength));
        }
        else
        {
            _logger.LogInformation("Response Body: {Body}", responseBody);
        }

        // Re-wrap the response content so the caller can read it
        response.Content = new StringContent(responseBody, System.Text.Encoding.UTF8, response.Content.Headers.ContentType?.MediaType ?? "application/json");

        return response;
    }
}
