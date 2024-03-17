using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BuildNumber
{
    public class BuildNumberFunction(ILogger<BuildNumberFunction> logger)
    {
        [Function(nameof(BuildNumberFunction))]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "next/{id}")] HttpRequest req, string id)
        {
            logger.LogInformation("Next build number requested for id: '{id}'", id);

            try
            {
                string connectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING")!;
                logger.LogInformation("Connection string read: {connStringrRead}", !string.IsNullOrEmpty(connectionString));
                using SqlConnection connection = new(connectionString);
                connection.Open();
                logger.LogInformation("DB connection opened");
                using SqlCommand readCommand = new("SELECT BuildNumber FROM [dbo].[build_numbers] WITH(ROWLOCK) WHERE BuildIdentifier = @id", connection);
                readCommand.Parameters.AddWithValue("@id", id);
                int nextBuildNumber = (int) readCommand.ExecuteScalar() + 1;
                using SqlCommand updateCommand = new("UPDATE [dbo].[build_numbers] WITH(ROWLOCK) SET BuildNumber = @nextBuildNumber WHERE BuildIdentifier = @id", connection);
                updateCommand.Parameters.AddWithValue("@nextBuildNumber", nextBuildNumber);
                updateCommand.Parameters.AddWithValue("@id", id);
                updateCommand.ExecuteNonQuery();
                logger.LogInformation("Next build number for id: '{id}' is {nextBuildNumber}", id, nextBuildNumber);
                return new OkObjectResult($$"""
                {
                    "buildId": "{{id}}",
                    "buildNumber": {{nextBuildNumber}}
                }
                """);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while getting next build number for id: '{id}'\n{exception}", id, ex.ToString());
                return new StatusCodeResult(500);
            }
        }
    }
}
