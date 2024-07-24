using Quartz;
using Quartz.Spi;
using Microsoft.Extensions.Hosting;

namespace Jobs;

public class QuartzHostedService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;

    public QuartzHostedService(ISchedulerFactory schedulerFactory, IJobFactory jobFactory)
    {
        _schedulerFactory = schedulerFactory;
        _jobFactory = jobFactory;
       }

    private IScheduler Scheduler { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        Scheduler.JobFactory = _jobFactory;

        IJobDetail confirmBy = JobBuilder.Create<QuartzApp>().Build();
        await Scheduler.ScheduleJob(confirmBy, ConfirmByGlobus(), cancellationToken);

        await Scheduler.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Scheduler.Shutdown(cancellationToken);
    }

    private ITrigger ConfirmByGlobus()
    {
        return TriggerBuilder.Create()
            .WithIdentity("confirmBy", "JobsWorker")
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(1)
                .RepeatForever())
            .Build();
    }
}