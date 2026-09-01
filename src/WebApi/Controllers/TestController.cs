using System.Diagnostics;
using System.Diagnostics.Metrics;
using BusinessMakerFramework.SourceGenerator.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class TestController(IMediator mediator) : ApiControllerBase(mediator)
{
    private static readonly ActivitySource ActivitySource = new("MyCompany.MyService");

    private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private readonly Counter<int> counter = MyMeter.CreateCounter<int>("Test-counter");

    [HttpPost(nameof(Test))]
    [AllowAnonymous]
    public IActionResult Test([FromServices] ILogger<TestController> logger)
    {
        using (Activity? activity = ActivitySource.StartActivity("ProcessOrder"))
        {
            // اضافه کردن Tag ها (Attributes در OTEL)
            _ = (activity?.SetTag("order.id", 12345));
            _ = (activity?.SetTag("order.customer", "Farshad"));

            // شبیه‌سازی کار
            Console.WriteLine("Processing order...");
            counter.Add(1);

            // اضافه کردن Event داخل Activity
            _ = (activity?.AddEvent(new ActivityEvent("OrderValidated")));

            // یک Activity تو در تو (Child Activity)
            using (Activity childActivity = ActivitySource.StartActivity("SaveOrderToDb"))
            {
                _ = (childActivity?.SetTag("db.system", "sqlserver"));
                _ = (childActivity?.SetTag("db.statement", "INSERT INTO Orders ..."));

                Console.WriteLine("Saving order to DB...");
            }
        }

        return Ok(true);
    }
}
