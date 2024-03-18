using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;

namespace Services.Tests;

[TestClass]
public class BuildNumberServiceTests
{
    private MsSqlContainer _dbContainer;

    private BuildNumberService BuildNumberService { get; set; }

    [TestInitialize]
    public async Task TestInitialize()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong_password_123!")
            .Build();

        await _dbContainer.StartAsync();
        DatabaseConnectionInfo connectionInfo = new(_dbContainer.GetConnectionString());
        BuildNumberService = new BuildNumberService(Mock.Of<ILogger<BuildNumberService>>(), connectionInfo);
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await _dbContainer?.StopAsync()!;
    }

    [TestMethod]
    public async Task GetNextBuildNumberAsync_ShouldReturnUniqueBuildNumbers_WhenInvokedInParallel()
    {
        // Arrange
        const string id = "concurrent-test";
        const int concurrentCalls = 100;
        await BuildNumberService.ResetAsync();
        await BuildNumberService.InitializeBuildIdentifier(id);

        // Act
        List<Task<Result<int>>> tasks = [];
        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(BuildNumberService.GetNextBuildNumberAsync(id));
        }
        await Task.WhenAll(tasks);

        // Assert
        List<int> values = tasks.Where(t => t is { IsCompletedSuccessfully: true, Result.IsSuccess: true }).Select(t => t.Result.Value).ToList();
        Assert.AreEqual(concurrentCalls, values.Distinct().Count());
    }

    [TestMethod]
    public async Task ResetInitializeAndSetBuildNumber_ShouldInitializeAndUpdateBuildNumber()
    {
        // Arrange
        const string id = "admin-test";

        // Act
        Result<bool> result1 = await BuildNumberService.ResetAsync();
        Result<bool> result2= await BuildNumberService.InitializeBuildIdentifier(id);
        Result<int> value1 = await BuildNumberService.GetNextBuildNumberAsync(id);
        Result<bool> result3 = await BuildNumberService.SetBuildNumberForBuildIdentifierAsync(id, 100);
        Result<int> value2 = await BuildNumberService.GetNextBuildNumberAsync(id);

        // Assert
        Assert.IsTrue(result1.IsSuccess);
        Assert.IsTrue(result2.IsSuccess);
        Assert.IsTrue(result3.IsSuccess);
        Assert.AreEqual(1, value1.Value);
        Assert.AreEqual(101, value2.Value);
    }

    [TestMethod]
    public async Task SetBuildNumberForBuildIdentifierAsync_ShouldNotUpdateBuildNumber_WhenBuildNumberIsLessThanCurrentValue()
    {
        // Arrange
        const string id = "set-number-test";
        await BuildNumberService.ResetAsync();
        await BuildNumberService.InitializeBuildIdentifier(id);
        await BuildNumberService.SetBuildNumberForBuildIdentifierAsync(id, 100);

        // Act
        Result<int> value1 = await BuildNumberService.GetNextBuildNumberAsync(id);
        Result<bool> result = await BuildNumberService.SetBuildNumberForBuildIdentifierAsync(id, 10);
        Result<int> value2 = await BuildNumberService.GetNextBuildNumberAsync(id);

        // Assert
        Assert.AreEqual(101, value1.Value);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(102, value2.Value);
    }
}