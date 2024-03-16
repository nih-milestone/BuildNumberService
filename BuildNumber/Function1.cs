using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BuildNumber
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("BuildNumber")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "next/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("Next build number requested for id: '{id}'", id);
            return new OkObjectResult($"Next build for {id} number is: 69");
        }
    }
}
