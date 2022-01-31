using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SMSStatusChange
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());

            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = await factory.GetScheduler();

            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<ChangeStatusJob>()
                .WithIdentity("job1", "group1")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10000)
                    //.WithIntervalInMinutes(30)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            //await Task.Delay(TimeSpan.FromSeconds(10));

            //await scheduler.Shutdown();

            Console.ReadKey();
        }
    }
}