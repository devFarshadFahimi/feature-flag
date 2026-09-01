namespace WebApi;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(token))
        {
            // Ensure it starts with "Bearer "
            if (!token.StartsWith("Bearer "))
            {
                token = "Bearer " + token;
            }

            request.Headers.Add("Authorization", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
