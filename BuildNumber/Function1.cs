using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BuildNumber
{
    public class Function1(ILogger<Function1> logger)
    {
        [Function("BuildNumber")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "next/{id}")] HttpRequest req, string id)
        {
            logger.LogInformation("Next build number requested for id: '{id}'", id);
            return new OkObjectResult($"Next build for {id} number is: 42");
        }
    }
}
