using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Services;

namespace BuildNumber;

public class BuildNumberFunction(ILogger<BuildNumberFunction> logger, BuildNumberService buildNumberService)
{
    [Function(nameof(BuildNumberFunction))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "next/{id}")] HttpRequest req, string id)
    {
        logger.LogInformation("Next build number requested for id: '{id}'", id);

        Result<int> result = await buildNumberService.GetNextBuildNumberAsync(id);
        return result.IsSuccess switch
        {
            true => new OkObjectResult($$"""
                        {
                            "buildId": "{{id}}",
                            "buildNumber": {{result.Value}}
                        }
                        """),
            false => new StatusCodeResult(500)
        };
    }
}