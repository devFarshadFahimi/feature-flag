using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

namespace WebApi.Handlers;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IDataProtectionProvider dataProtectionProvider)
    {
        var dataProtector = dataProtectionProvider.CreateProtector("LoggingPipeline");
        Log.Information($"---------------------- Starting Service Input & Output Scope ----------------------");

        // Read and log the request body data
        string requestBodyPayload = await ReadRequestBody(context);
        Log.Information("Request Payload {Payload} {TraceId}", requestBodyPayload, context.TraceIdentifier);

        // Copy a pointer to the original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            // Point the response body to a memory stream
            context.Response.Body = responseBody;

            await _next(context);

            // Read and log the response body data
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string responseBodyPayload = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var responseBodyEncrypted = dataProtector.Protect(responseBodyPayload);

            Log.Information("Response {StatusCode} {Body} {TraceId}", context.Response?.StatusCode, responseBodyEncrypted, context.TraceIdentifier);

            // Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
            await responseBody.CopyToAsync(originalBodyStream);
        }

        Log.Information($"---------------------- Finishing Service Input & Output Scope ----------------------");
    }

    private async Task<string> ReadRequestBody(HttpContext context)
    {
        context.Request.EnableBuffering();

        var buffer = new byte[Convert.ToInt32(context.Request.ContentLength)];
        await context.Request.Body.ReadExactlyAsync(buffer);
        string bodyAsText = Encoding.UTF8.GetString(buffer);
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        return bodyAsText;
    }
}