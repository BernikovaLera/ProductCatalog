using Catalog.Data;
using Quartz;

namespace Catalog.Workers;

public class DeleteFromDatabase(ApplicationContext db)
{
    public async Task Execute(IJobExecutionContext context)
    {
        var softDeletedRecords =  db.Articles.Where(e => e.IsDeleted && e.UpdatedAt < DateTime.UtcNow.AddDays(-5)).ToList();
        db.Articles.RemoveRange(softDeletedRecords);
        await db.SaveChangesAsync();
    }
}