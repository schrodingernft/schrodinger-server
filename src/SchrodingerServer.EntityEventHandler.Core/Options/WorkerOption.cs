using System.Collections.Generic;

namespace SchrodingerServer.EntityEventHandler.Core.Options;

public class WorkerOptions
{
    public const string DefaultCron = "0 0/3 * * * ?";

    public string[] ChainIds { get; set; }
    
    public string BizDate { get; set; }

    public Dictionary<string, Worker> Workers { get; set; } = new Dictionary<string, Worker>();
}


public class Worker
{
    public string Cron { get; set; } = WorkerOptions.DefaultCron;
}