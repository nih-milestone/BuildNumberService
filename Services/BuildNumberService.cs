using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Services;

public class BuildNumberService(ILogger<BuildNumberService> logger, DatabaseConnectionInfo connectionInfo)
{

    public async Task<Result<bool>> ResetAsync()
    {
        try
        {
            await using SqlConnection connection = new(connectionInfo.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand createCommand = new("DROP TABLE IF EXISTS [dbo].[build_numbers]; CREATE TABLE [dbo].[build_numbers] (ID INT IDENTITY(1,1) PRIMARY KEY, BuildIdentifier VARCHAR(255) NOT NULL, BuildNumber INT NOT NULL DEFAULT 0)", connection);
            await createCommand.ExecuteNonQueryAsync();
            logger.LogInformation("Reset service");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            logger.LogError(ex, "Error while resetting service\n{exception}", ex.ToString());
            return Result<bool>.Failure(new Error("DB_ERROR", ex.Message));
        }
    }

    public async Task<Result<bool>> InitializeBuildIdentifier(string id)
    {
        try
        {
            await using SqlConnection connection = new(connectionInfo.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand insertCommand = new("INSERT INTO [dbo].[build_numbers] (BuildIdentifier) VALUES (@id)", connection);
            insertCommand.Parameters.AddWithValue("@id", id);
            await insertCommand.ExecuteNonQueryAsync();
            logger.LogInformation("Initialized build identifier: '{id}'", id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            logger.LogError(ex, "Error while initializing build identifier: '{id}'\n{exception}", id, ex.ToString());
            return Result<bool>.Failure(new Error("DB_ERROR", ex.Message));
        }
    }

    public async Task<Result<bool>> RemoveBuildIdentifier(string id)
    {
        try
        {
            await using SqlConnection connection = new(connectionInfo.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand deleteCommand = new("DELETE FROM [dbo].[build_numbers] WHERE BuildIdentifier = @id", connection);
            deleteCommand.Parameters.AddWithValue("@id", id);
            await deleteCommand.ExecuteNonQueryAsync();
            logger.LogInformation("Removed build identifier: '{id}'", id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            logger.LogError(ex, "Error while removing build identifier: '{id}'\n{exception}", id, ex.ToString());
            return Result<bool>.Failure(new Error("DB_ERROR", ex.Message));
        }
    }

    public async Task<Result<bool>> SetBuildNumberForBuildIdentifierAsync(string id, int buildNumber)
    {
        try
        {
            await using SqlConnection connection = new(connectionInfo.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand updateCommand = new("UPDATE [dbo].[build_numbers] SET BuildNumber = @buildNumber WHERE BuildIdentifier = @id AND BuildNumber < @buildNumber", connection);
            updateCommand.Parameters.AddWithValue("@id", id);
            updateCommand.Parameters.AddWithValue("@buildNumber", buildNumber);
            if (await updateCommand.ExecuteNonQueryAsync() == 1)
            {
                logger.LogInformation("Set build number '{buildNumber}' for id: '{id}'", buildNumber, id);
                return Result<bool>.Success(true);
            }
            logger.LogWarning("Unable to set build number '{buildNumber}' for id: '{id}'", buildNumber, id);
            return Result<bool>.Failure(new Error("DB_ERROR", "Unable to set BuildNumber. Ensure supplied buildNumber is larger than the current build number."));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            logger.LogError(ex, "Error while setting build number '{buildNumber}' for id: '{id}'\n{exception}", buildNumber, id, ex.ToString());
            return Result<bool>.Failure(new Error("DB_ERROR", ex.Message));
        }
    }

    public async Task<Result<int>> GetNextBuildNumberAsync(string id)
    {
        try
        {
            await using SqlConnection connection = new(connectionInfo.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand updateCommand = new("UPDATE [dbo].[build_numbers] SET BuildNumber += 1 OUTPUT INSERTED.BuildNumber WHERE BuildIdentifier = @id ", connection);
            updateCommand.Parameters.AddWithValue("@id", id);
            if (await updateCommand.ExecuteScalarAsync() is not int nextBuildNumber)
            {
                logger.LogError("No build number found for id: '{id}'", id);
                return Result<int>.Failure(new Error("NOT_FOUND", "No build number found for the provided id"));
            }
            logger.LogInformation("Next build number for id: '{id}' is {nextBuildNumber}", id, nextBuildNumber);
            return Result<int>.Success(nextBuildNumber);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            logger.LogError(ex, "Error while getting next build number for id: '{id}'\n{exception}", id, ex.ToString());
            return Result<int>.Failure(new Error("DB_ERROR", ex.Message));
        }
    }
}