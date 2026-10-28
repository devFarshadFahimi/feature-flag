using BusinessMakerFramework.Domain.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Handlers;

public sealed class ApiProblemHandler(ILogger<ApiProblemHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            UnauthorizedAccessException => (401, "نشست کاربری معتبر نیست."),
            System.Security.SecurityException => (403, "اجازه انجام این عملیات را ندارید."),
            KeyNotFoundException => (404, "رکورد درخواستی یافت نشد."),
            DbUpdateConcurrencyException => (409, "اطلاعات تغییر کرده است؛ دوباره بارگذاری کنید."),
            DbUpdateException => (409, "ذخیره اطلاعات با وضعیت فعلی سازگار نیست."),
            ArgumentException => (400, "اطلاعات درخواست معتبر نیست."),
            BusinessMakerFramework.Shared.Utilities.Exceptions.ValidationBehaviourException => (400, "اطلاعات درخواست معتبر نیست."),
            InvalidEntityStateException => (409, "عملیات با وضعیت فعلی سازگار نیست."),
            _ => (503, "سرویس موقتاً در دسترس نیست.")
        };

        if (status == 503)
        {
            logger.LogError(exception, "خطا در پردازش درخواست");
        }

        if (status is 400 or 409 && exception is not ArgumentException { ParamName: not null } && exception.Message.Any(c => c >= '\u0600' && c <= '\u06ff'))
        {
            message = exception.Message;
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = message }, cancellationToken);

        return true;
    }
}
