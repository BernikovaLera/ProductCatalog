using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Jobs;

public class PeriodicTask : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobFactory _jobFactory;

    public PeriodicTask(ISchedulerFactory schedulerFactory, IJobFactory jobFactory, IScheduler scheduler)
    {
        _schedulerFactory = schedulerFactory;
        _jobFactory = jobFactory;
        Scheduler = scheduler;
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
        if (Scheduler != null) await Scheduler?.Shutdown(cancellationToken)!;
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
    
    // public static async Task Start()
    // {
    //     try
    //     {
    //         ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
    //         IScheduler scheduler = schedulerFactory.GetScheduler().Result;
    //         await scheduler.Start();
    //
    //         IJobDetail job = JobBuilder.Create<QuartzApp>()
    //             .WithIdentity("QuartzApp", "Group1")
    //             .Build();
    //
    //         ITrigger trigger = TriggerBuilder.Create()
    //             .WithIdentity("QuartzAppTrigger1", "Group1")
    //             .StartNow()
    //             .WithSimpleSchedule(x => x
    //                 .WithIntervalInSeconds(10)
    //                 .RepeatForever())
    //             .Build();
    //
    //         await scheduler.ScheduleJob(job, trigger);
    //
    //         Console.WriteLine("Задание успешно запланировано.");
    //         Console.ReadLine();
    //     }
    //     catch (AggregateException ex)
    //     {
    //         Console.WriteLine($"Ошибка при запуске задания: {ex.Message}");
    //     }
    // }
}