using Catalog.Data;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Jobs;

public class QuartzApp : IJob
{
    private readonly ApplicationContext _dbContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QuartzApp(ApplicationContext dbContext, IServiceScopeFactory serviceScopeFactory)
    {
        _dbContext = dbContext;
        _serviceScopeFactory = serviceScopeFactory;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    
        var softDeletedRecords =  _dbContext.Articles.Where(e => e.IsDeleted && e.UpdatedAt < DateTime.UtcNow.AddDays(-5)).ToList();
        foreach (var softDeletedRecord in softDeletedRecords) _dbContext.Articles.Remove(softDeletedRecord);
        await _dbContext.SaveChangesAsync();
    
        Console.WriteLine("Выолнение задания...");
        
    }
}