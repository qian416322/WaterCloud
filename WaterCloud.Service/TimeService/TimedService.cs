﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WaterCloud.Code;
using WaterCloud.Domain.SystemSecurity;
using WaterCloud.Service.SystemSecurity;

namespace WaterCloud.Service.TimeService
{

    /// <summary>
    /// 定时任务触发器，已废除
    /// </summary>
    public class TimedService : BackgroundService
    {
        /**
         *  
            需要在Startup.cs 里面注册
         *  public void ConfigureServices(IServiceCollection services)
            { 
                services.AddHostedService<TimedService>();
                或者
                services.AddTransient<IHostedService, TimedService>();
            }
         * */

        private readonly ILogger _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        //定时器
        private Timer _timer;

        //private Timer _timer;

        public TimedService(ILogger<TimedService> logger,IHostingEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment=hostingEnvironment;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("servers start");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));//间隔30秒
            _logger.LogInformation("servers end");
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            //执行任务
            ServerStateEntity entity = new ServerStateEntity();
            var computer = ComputerHelper.GetComputerInfo();
            entity.F_ARM = computer.RAMRate;
            entity.F_CPU = computer.CPURate;
            entity.F_IIS = "0";
            entity.F_WebSite = _hostingEnvironment.ContentRootPath;
            new ServerStateService().SubmitForm(entity);
        }

        public override void Dispose()
        {
            base.Dispose();
            _timer?.Dispose();
        }

    }
}

