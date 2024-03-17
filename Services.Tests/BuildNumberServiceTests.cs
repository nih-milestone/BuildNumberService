using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Services.Tests;

[TestClass]
public class BuildNumberServiceTests
{
    private static readonly DatabaseConnectionInfo ConnectionInfo = new("--- PASTE ADO.NET Connection String from Azure to test locally - Further changes needed to support running in pipeline ---");
    
    private const string BuildIdentifier = "test-build-id_5E156186-C18B-43FE-B2FB-020597A69CD3";

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        BuildNumberService buildNumberService = new(Mock.Of<ILogger<BuildNumberService>>(), ConnectionInfo);
        await buildNumberService.InitializeBuildIdentifier(BuildIdentifier);
    }

    [ClassCleanup]
    public static async Task Cleanup()
    {
        BuildNumberService buildNumberService = new(Mock.Of<ILogger<BuildNumberService>>(), ConnectionInfo);
        await buildNumberService.RemoveBuildIdentifier(BuildIdentifier);
    }

    [TestMethod]
    public async Task GetNextBuildNumberAsync_ShouldReturnUniqueBuildNumbers_WhenInvokedInParallel()
    {
        // Arrange
        const int concurrentCalls = 10;
        BuildNumberService buildNumberService = new(Mock.Of<ILogger<BuildNumberService>>(), ConnectionInfo);

        // Act
        List<Task<Result<int>>> tasks = [];
        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(buildNumberService.GetNextBuildNumberAsync(BuildIdentifier));
        }
        await Task.WhenAll(tasks);

        // Assert
        List<int> values = tasks.Where(t => t is { IsCompletedSuccessfully: true, Result.IsSuccess: true }).Select(t => t.Result.Value).ToList();
        Debug.WriteLine($"Values are: {string.Join(", ", values.Order())}");
        Assert.AreEqual(concurrentCalls, values.Distinct().Count());
    }
}