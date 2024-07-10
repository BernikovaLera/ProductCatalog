using Quartz;
using Quartz.Spi;

namespace Jobs;

public class SingletonJobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SingletonJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobDetail = bundle.JobDetail;
        var job = (IJob) _serviceProvider.GetService(jobDetail.JobType)!;
        return job;
    }

    public void ReturnJob(IJob job)
    {
    }
}