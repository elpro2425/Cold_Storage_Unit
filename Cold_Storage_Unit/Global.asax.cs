
using Cold_Storage_Unit.Models;
using Quartz;
using Quartz.Impl;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;


namespace Cold_Storage_Unit
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //StartQuartzScheduler();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

           
        }

        private void StartQuartzScheduler()
        {
            IScheduler scheduler = Quartz.Impl.StdSchedulerFactory.GetDefaultScheduler().Result;
            scheduler.Start();

            IJobDetail job = JobBuilder.Create<EmailService>()
                .WithIdentity("dailyReportJob", "group1")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                 .WithIdentity("dailyReportTrigger", "group1")
                 .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(13, 45)) 
                 .Build();


            scheduler.ScheduleJob(job, trigger);
        }

    }
}
