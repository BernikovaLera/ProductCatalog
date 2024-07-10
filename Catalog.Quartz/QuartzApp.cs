using Quartz;
using Catalog.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Jobs;

public class QuartzApp : IJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<QuartzApp> _logger;

    public QuartzApp(IServiceScopeFactory serviceScopeFactory, ILogger<QuartzApp> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Start Job operation");

            using var scope = _serviceScopeFactory.CreateScope();
            await using ApplicationContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            var softDeletedRecords = dbContext.Articles
                .Where(e => e.IsDeleted && e.UpdatedAt < DateTime.UtcNow.AddDays(-5))
                .ToList();
            foreach (var softDeletedRecord in softDeletedRecords) dbContext.Articles.Remove(softDeletedRecord);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            throw;
        }
        finally
        {
            _logger.LogInformation("Finished Job operation");
        }
    }
}