using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BuildNumberAdmin
{
    public class Function1(ILogger<Function1> logger)
    {
        [Function("BuildNumberAdmin")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "set/{id}")] HttpRequest req, string id)
        {
            string newValueString = await new StreamReader(req.Body).ReadToEndAsync();
            if (!int.TryParse(newValueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int newValue))
                return new BadRequestObjectResult("Invalid build number. Only integers are allowed");

            logger.LogInformation("Build number set to {newValue} for id: '{id}'", newValue, id);
            return new OkObjectResult($"Set next build number for {id} to {newValue}");
        }
    }
}
